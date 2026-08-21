using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Settings;

public sealed class AccessSettingsUseCase(
    ISettingsRepository repository,
    IUserRepository users,
    ITenantRepository tenants,
    IEmailSender emailSender,
    IOptions<FrontendOptions> frontendOptions,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IAccessSettingsUseCase
{
    public async Task<Result<IReadOnlyCollection<EnabledModulePermissionsDto>>> GetPermissionsAsync(Guid tenantId, CancellationToken cancellationToken)
        => Result<IReadOnlyCollection<EnabledModulePermissionsDto>>.Success(await repository.GetEnabledPermissionCatalogAsync(tenantId, cancellationToken));

    public async Task<Result<IReadOnlyCollection<SettingsRoleDto>>> ListRolesAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken)
        => Result<IReadOnlyCollection<SettingsRoleDto>>.Success((await repository.GetRolesAsync(tenantId, includeInactive, cancellationToken)).Select(MapRole).ToArray());

    public async Task<Result<SettingsRoleDto>> CreateRoleAsync(Guid tenantId, SaveSettingsRoleRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRoleRequest(tenantId, request, null, cancellationToken);
        if (validation.Error is not null) return Result<SettingsRoleDto>.Failure(validation.Error);
        var now = clock.UtcNow;
        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = CreateRoleCode(request.Name),
            Name = SettingsDefaults.Normalize(request.Name),
            Description = Clean(request.Description),
            IsSystem = false,
            IsActive = true,
            CreatedAt = now
        };
        await repository.AddRoleAsync(role, cancellationToken);
        await repository.ReplaceRolePermissionsAsync(role.Id, validation.Permissions, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        role.RolePermissions = validation.Permissions.Select(code => new RolePermission { RoleId = role.Id, Permission = new Permission { Code = code } }).ToArray();
        return Result<SettingsRoleDto>.Success(MapRole(role));
    }

    public async Task<Result<SettingsRoleDto>> UpdateRoleAsync(Guid tenantId, Guid roleId, SaveSettingsRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleForUpdateAsync(tenantId, roleId, cancellationToken);
        if (role is null) return Result<SettingsRoleDto>.Failure(Errors.SettingsRoleNotFound);
        if (role.IsSystem) return Result<SettingsRoleDto>.Failure(Errors.SettingsRoleProtected);
        var validation = await ValidateRoleRequest(tenantId, request, roleId, cancellationToken);
        if (validation.Error is not null) return Result<SettingsRoleDto>.Failure(validation.Error);
        role.Name = SettingsDefaults.Normalize(request.Name); role.Description = Clean(request.Description);
        await repository.ReplaceRolePermissionsAsync(role.Id, validation.Permissions, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        role.RolePermissions = validation.Permissions.Select(code => new RolePermission { RoleId = role.Id, Permission = new Permission { Code = code } }).ToArray();
        return Result<SettingsRoleDto>.Success(MapRole(role));
    }

    public async Task<Result<SettingsRoleDto>> SetRoleStatusAsync(Guid tenantId, Guid roleId, SetSettingsRoleStatusRequest request, CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleForUpdateAsync(tenantId, roleId, cancellationToken);
        if (role is null) return Result<SettingsRoleDto>.Failure(Errors.SettingsRoleNotFound);
        if (role.IsSystem) return Result<SettingsRoleDto>.Failure(Errors.SettingsRoleProtected);
        role.IsActive = request.IsActive; await unitOfWork.SaveChangesAsync(cancellationToken); return Result<SettingsRoleDto>.Success(MapRole(role));
    }

    public async Task<Result> DeleteRoleAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleForUpdateAsync(tenantId, roleId, cancellationToken);
        if (role is null) return Result.Failure(Errors.SettingsRoleNotFound);
        if (role.IsSystem) return Result.Failure(Errors.SettingsRoleProtected);
        if (await repository.RoleIsInUseAsync(tenantId, roleId, cancellationToken)) return Result.Failure(Errors.SettingsRoleInUse);
        repository.RemoveRole(role); await unitOfWork.SaveChangesAsync(cancellationToken); return Result.Success();
    }

    public async Task<Result<IReadOnlyCollection<OrganizationUserDto>>> ListUsersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var memberships = await repository.GetMembershipsAsync(tenantId, cancellationToken);
        var invitations = await repository.GetInvitationsAsync(tenantId, cancellationToken);
        var output = memberships.Select(MapMembership).Concat(invitations.Select(MapInvitation)).OrderBy(item => item.Email).ToArray();
        return Result<IReadOnlyCollection<OrganizationUserDto>>.Success(output);
    }

    public async Task<Result<OrganizationUserDto>> InviteUserAsync(Guid tenantId, Guid invitedByUserId, InviteOrganizationUserRequest request, CancellationToken cancellationToken)
    {
        var normalized = LoginUseCase.NormalizeEmail(request.Email);
        var role = await repository.GetRoleForUpdateAsync(tenantId, request.RoleId, cancellationToken);
        if (role is null || !role.IsActive) return Result<OrganizationUserDto>.Failure(Errors.SettingsRoleNotFound);
        var tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<OrganizationUserDto>.Failure(Errors.TenantNotFound);
        var existingInvitation = await repository.GetInvitationByEmailAsync(tenantId, normalized, cancellationToken);
        var user = await users.GetByNormalizedEmailAsync(normalized, cancellationToken);
        if (user is not null)
        {
            var existingMemberships = await repository.GetMembershipsAsync(tenantId, cancellationToken);
            if (existingMemberships.Any(item => item.UserId == user.Id)) return Result<OrganizationUserDto>.Failure(Errors.OrganizationInvitationExists);
            var membership = new TenantMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id, RoleId = role.Id, IsActive = true, CreatedAt = clock.UtcNow, User = user, Role = role };
            await tenants.AddMembershipAsync(membership, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await SendInvitationAsync(request.Email, tenant.Name, true, cancellationToken);
            return Result<OrganizationUserDto>.Success(MapMembership(membership));
        }
        if (existingInvitation is not null && existingInvitation.AcceptedAt is null && existingInvitation.RevokedAt is null)
            return Result<OrganizationUserDto>.Failure(Errors.OrganizationInvitationExists);
        var invitation = existingInvitation ?? new TenantInvitation { Id = Guid.NewGuid(), TenantId = tenantId, NormalizedEmail = normalized, CreatedAt = clock.UtcNow };
        invitation.Email = request.Email.Trim(); invitation.RoleId = role.Id; invitation.Role = role; invitation.InvitedByUserId = invitedByUserId; invitation.AcceptedAt = null; invitation.RevokedAt = null;
        if (existingInvitation is null) await repository.AddInvitationAsync(invitation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await SendInvitationAsync(request.Email, tenant.Name, false, cancellationToken);
        return Result<OrganizationUserDto>.Success(MapInvitation(invitation));
    }

    public async Task<Result<OrganizationUserDto>> UpdateUserAsync(Guid tenantId, Guid membershipId, UpdateOrganizationUserRequest request, CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipForUpdateAsync(tenantId, membershipId, cancellationToken);
        if (membership is null) return Result<OrganizationUserDto>.Failure(Errors.OrganizationUserNotFound);
        var role = await repository.GetRoleForUpdateAsync(tenantId, request.RoleId, cancellationToken);
        if (role is null || !role.IsActive) return Result<OrganizationUserDto>.Failure(Errors.SettingsRoleNotFound);
        if (membership.Role.Code == "TENANT_OWNER" && (role.Code != "TENANT_OWNER" || !request.IsActive)
            && !await repository.HasAnotherActiveOwnerAsync(tenantId, membership.Id, clock.UtcNow, cancellationToken))
            return Result<OrganizationUserDto>.Failure(Errors.OrganizationOwnerRequired);
        membership.RoleId = role.Id; membership.Role = role;
        membership.IsActive = request.IsActive;
        membership.DisabledUntil = request.IsActive ? null : request.DisabledUntil;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OrganizationUserDto>.Success(MapMembership(membership));
    }

    public async Task<Result> DeleteUserAsync(Guid tenantId, Guid currentUserId, Guid entryId, CancellationToken cancellationToken)
    {
        var membership = await repository.GetMembershipForUpdateAsync(tenantId, entryId, cancellationToken);
        if (membership is not null)
        {
            if (membership.UserId == currentUserId) return Result.Failure(Errors.OrganizationOwnerRequired);
            if (membership.Role.Code == "TENANT_OWNER" && !await repository.HasAnotherActiveOwnerAsync(tenantId, membership.Id, clock.UtcNow, cancellationToken))
                return Result.Failure(Errors.OrganizationOwnerRequired);
            repository.RemoveMembership(membership); await unitOfWork.SaveChangesAsync(cancellationToken); return Result.Success();
        }
        var invitation = await repository.GetInvitationAsync(tenantId, entryId, cancellationToken);
        if (invitation is null) return Result.Failure(Errors.OrganizationUserNotFound);
        repository.RemoveInvitation(invitation); await unitOfWork.SaveChangesAsync(cancellationToken); return Result.Success();
    }

    private async Task<(Error? Error, IReadOnlyCollection<string> Permissions)> ValidateRoleRequest(Guid tenantId, SaveSettingsRoleRequest request, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return (Errors.Validation, []);
        var name = SettingsDefaults.Normalize(request.Name); var code = CreateRoleCode(name);
        if (await repository.RoleCodeOrNameExistsAsync(tenantId, code, name, excludedId, cancellationToken)) return (Errors.SettingsRoleAlreadyExists, []);
        var requested = (request.Permissions ?? []).Where(codeValue => !string.IsNullOrWhiteSpace(codeValue)).Distinct(StringComparer.Ordinal).ToArray();
        var enabled = await repository.GetEnabledPermissionCodesAsync(tenantId, cancellationToken);
        if (requested.Except(enabled, StringComparer.Ordinal).Any()) return (Errors.PermissionNotEnabled, []);
        return (null, requested);
    }

    private static string CreateRoleCode(string name) => $"CUSTOM_{Regex.Replace(SettingsDefaults.NormalizeKey(name), "[^A-Z0-9]+", "_").Trim('_')}";
    private Task SendInvitationAsync(string email, string organizationName, bool accountExists, CancellationToken cancellationToken)
    {
        var baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        var route = accountExists ? "login" : "register";
        var link = $"{baseUrl}/{route}?email={Uri.EscapeDataString(email.Trim())}";
        return emailSender.SendOrganizationInvitationAsync(email.Trim(), "es", organizationName, link, cancellationToken);
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static SettingsRoleDto MapRole(Role role) => new(role.Id, role.Code, role.Name, role.Description, role.IsSystem, role.IsActive,
        role.RolePermissions.Select(item => item.Permission.Code).OrderBy(code => code).ToArray());
    private OrganizationUserDto MapMembership(TenantMembership item) => new(item.Id, item.Id, null, item.User.Email, item.User.FirstName, item.User.LastName,
        item.RoleId, item.Role.Name, item.IsEnabledAt(clock.UtcNow) ? "ACTIVE" : "DISABLED", item.DisabledUntil, item.CreatedAt);
    private static OrganizationUserDto MapInvitation(TenantInvitation item) => new(item.Id, null, item.Id, item.Email, null, null,
        item.RoleId, item.Role.Name, "PENDING", null, item.CreatedAt);
}
