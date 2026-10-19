using Microsoft.Extensions.Options;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.OrganizationLinks;

public sealed class OrganizationAccessLinkUseCase(
    IOrganizationAccessLinkRepository repository,
    ITenantRepository tenants,
    IRoleRepository roles,
    IPermissionRepository permissions,
    ITokenGenerator tokenGenerator,
    IOrganizationAccessLinkCodeProtector codeProtector,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    IOptions<FrontendOptions> frontendOptions) : IOrganizationAccessLinkUseCase
{
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(12);

    public async Task<Result<CreateJoinRequestResponse>> CreateJoinRequestAsync(Guid userId, CancellationToken cancellationToken)
    {
        var token = tokenGenerator.Generate();
        var link = new OrganizationAccessLink
        {
            Id = Guid.NewGuid(), CodeHash = tokenGenerator.Hash(token), Kind = OrganizationAccessLinkKind.JoinRequest,
            ProtectedCode = codeProtector.Protect(token),
            RequestedByUserId = userId, CreatedByUserId = userId, CreatedAt = clock.UtcNow,
            ExpiresAt = clock.UtcNow.Add(LinkLifetime)
        };
        await repository.AddAsync(link, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CreateJoinRequestResponse>.Success(new CreateJoinRequestResponse(MapCreated(token, link, null, null)));
    }

    public async Task<Result<CreatedOrganizationAccessLinkDto>> CreateInvitationAsync(Guid tenantId, Guid userId, CreateOrganizationInvitationLinkRequest request, CancellationToken cancellationToken)
    {
        var role = await roles.GetByIdAsync(request.RoleId, cancellationToken);
        var tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive || role is null || role.TenantId != tenantId || !role.IsActive)
            return Result<CreatedOrganizationAccessLinkDto>.Failure(Errors.SettingsRoleNotFound);

        var token = tokenGenerator.Generate();
        var link = new OrganizationAccessLink
        {
            Id = Guid.NewGuid(), CodeHash = tokenGenerator.Hash(token), Kind = OrganizationAccessLinkKind.Invitation,
            ProtectedCode = codeProtector.Protect(token),
            TenantId = tenantId, RoleId = request.RoleId, CreatedByUserId = userId, CreatedAt = clock.UtcNow,
            ExpiresAt = clock.UtcNow.Add(LinkLifetime)
        };
        await repository.AddAsync(link, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CreatedOrganizationAccessLinkDto>.Success(MapCreated(token, link, tenant.Name, role.Name));
    }

    public async Task<Result<IReadOnlyCollection<OrganizationAccessLinkSummaryDto>>> ListInvitationsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var links = await repository.GetOpenInvitationsAsync(tenantId, clock.UtcNow, cancellationToken);
        var result = new List<OrganizationAccessLinkSummaryDto>();
        var upgradedLegacyLinks = false;
        foreach (var link in links)
        {
            var role = link.RoleId.HasValue ? await roles.GetByIdAsync(link.RoleId.Value, cancellationToken) : null;
            if (link.ProtectedCode is null)
            {
                var replacementCode = tokenGenerator.Generate();
                link.CodeHash = tokenGenerator.Hash(replacementCode);
                link.ProtectedCode = codeProtector.Protect(replacementCode);
                upgradedLegacyLinks = true;
            }
            string? url = null;
            if (link.ProtectedCode is not null)
            {
                try
                {
                    url = MapCreated(codeProtector.Unprotect(link.ProtectedCode), link, link.Tenant?.Name, role?.Name).Url;
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    // Links created before protected-code storage remain usable, but cannot be reopened from the list.
                }
            }
            result.Add(new OrganizationAccessLinkSummaryDto(link.Id, "INVITATION", link.Tenant?.Name, role?.Name, link.ExpiresAt, url));
        }
        if (upgradedLegacyLinks) await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<IReadOnlyCollection<OrganizationAccessLinkSummaryDto>>.Success(result);
    }

    public async Task<Result<OrganizationAccessLinkDto>> PreviewAsync(string code, Guid? userId, CancellationToken cancellationToken)
    {
        var link = await repository.GetByCodeHashAsync(tokenGenerator.Hash(code.Trim()), cancellationToken);
        if (link is null) return Result<OrganizationAccessLinkDto>.Failure(Errors.OrganizationAccessLinkInvalid);
        if (link.AcceptedAt.HasValue) return Result<OrganizationAccessLinkDto>.Failure(Errors.OrganizationAccessLinkAccepted);
        if (link.ExpiresAt <= clock.UtcNow) return Result<OrganizationAccessLinkDto>.Failure(Errors.OrganizationAccessLinkExpired);
        var role = link.RoleId.HasValue ? await roles.GetByIdAsync(link.RoleId.Value, cancellationToken) : null;
        IReadOnlyCollection<TenantDto>? availableOrganizations = null;
        if (link.Kind == OrganizationAccessLinkKind.JoinRequest && userId.HasValue)
        {
            var memberships = await tenants.GetForUserAsync(userId.Value, cancellationToken);
            var manageableOrganizations = new List<TenantDto>();
            foreach (var membership in memberships)
            {
                if (await permissions.RoleHasPermissionAsync(
                        membership.Id,
                        membership.RoleId,
                        PermissionCodes.SettingsUsersManage,
                        cancellationToken))
                {
                    manageableOrganizations.Add(membership);
                }
            }

            availableOrganizations = manageableOrganizations;
        }

        return Result<OrganizationAccessLinkDto>.Success(MapPreview(code, link, role?.Name, availableOrganizations));
    }

    public async Task<Result<AcceptOrganizationAccessLinkResponse>> AcceptAsync(Guid userId, string code, AcceptOrganizationAccessLinkRequest request, CancellationToken cancellationToken)
    {
        var link = await repository.GetByCodeHashAsync(tokenGenerator.Hash(code.Trim()), cancellationToken);
        if (link is null) return Result<AcceptOrganizationAccessLinkResponse>.Failure(Errors.OrganizationAccessLinkInvalid);
        if (link.AcceptedAt.HasValue) return Result<AcceptOrganizationAccessLinkResponse>.Failure(Errors.OrganizationAccessLinkAccepted);
        if (link.ExpiresAt <= clock.UtcNow) return Result<AcceptOrganizationAccessLinkResponse>.Failure(Errors.OrganizationAccessLinkExpired);

        var tenantId = link.TenantId ?? request.TenantId;
        var roleId = link.RoleId ?? request.RoleId;
        if (!tenantId.HasValue || !roleId.HasValue) return Result<AcceptOrganizationAccessLinkResponse>.Failure(Errors.Validation);
        var tenant = await tenants.GetByIdAsync(tenantId.Value, cancellationToken);
        var role = await roles.GetByIdAsync(roleId.Value, cancellationToken);
        if (tenant is null || !tenant.IsActive || role is null || !role.IsActive || role.TenantId != tenantId)
            return Result<AcceptOrganizationAccessLinkResponse>.Failure(Errors.TenantNotFound);

        var acceptor = await tenants.GetMembershipAsync(userId, tenantId.Value, cancellationToken);
        if (link.Kind == OrganizationAccessLinkKind.JoinRequest)
        {
            if (acceptor is null || !acceptor.IsEnabledAt(clock.UtcNow)
                || !await permissions.RoleHasPermissionAsync(tenantId.Value, acceptor.RoleId, "settings.users.manage", cancellationToken))
                return Result<AcceptOrganizationAccessLinkResponse>.Failure(Errors.TenantAccessDenied);
            var requesterId = link.RequestedByUserId!.Value;
            var requesterMembership = await tenants.GetMembershipAsync(requesterId, tenantId.Value, cancellationToken);
            if (requesterMembership is null)
                await tenants.AddMembershipAsync(new TenantMembership { Id = Guid.NewGuid(), UserId = requesterId, TenantId = tenantId.Value, RoleId = roleId.Value, CreatedAt = clock.UtcNow }, cancellationToken);
            else
            {
                requesterMembership.RoleId = roleId.Value;
                requesterMembership.IsActive = true;
                requesterMembership.DisabledUntil = null;
            }
        }
        else
        {
            if (acceptor is not null && acceptor.IsEnabledAt(clock.UtcNow))
                return Result<AcceptOrganizationAccessLinkResponse>.Failure(Errors.OrganizationInvitationExists);
            if (acceptor is null)
                await tenants.AddMembershipAsync(new TenantMembership { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId.Value, RoleId = roleId.Value, CreatedAt = clock.UtcNow }, cancellationToken);
            else
            {
                acceptor.RoleId = roleId.Value;
                acceptor.IsActive = true;
                acceptor.DisabledUntil = null;
            }
        }

        link.AcceptedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AcceptOrganizationAccessLinkResponse>.Success(new AcceptOrganizationAccessLinkResponse(
            link.Kind.ToString().ToUpperInvariant(), tenantId.Value, tenant.Name, role.Name, true));
    }

    private CreatedOrganizationAccessLinkDto MapCreated(string token, OrganizationAccessLink link, string? organizationName, string? roleName)
        => new(token, $"{frontendOptions.Value.BaseUrl.TrimEnd('/')}/organization-links/{token}", link.Kind.ToString().ToUpperInvariant(), organizationName, roleName, link.ExpiresAt);

    private static OrganizationAccessLinkDto MapPreview(
        string code,
        OrganizationAccessLink link,
        string? roleName,
        IReadOnlyCollection<TenantDto>? availableOrganizations = null)
        => new(code, link.Kind.ToString().ToUpperInvariant(), link.Tenant?.Name, roleName,
            link.RequestedByUser is null ? null : $"{link.RequestedByUser.FirstName} {link.RequestedByUser.LastName}".Trim(),
            link.ExpiresAt, false, false, availableOrganizations);
}
