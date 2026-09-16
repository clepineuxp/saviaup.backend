using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class SettingsRepository(
    PlatformDbContext platformContext,
    ApplicationDbContext appContext) : ISettingsRepository
{
    public Task<Tenant?> GetTenantForUpdateAsync(Guid tenantId, CancellationToken cancellationToken)
        => platformContext.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);

    public async Task<IReadOnlyCollection<OrganizationParameter>> GetParametersAsync(Guid tenantId, CancellationToken cancellationToken)
        => await appContext.OrganizationParameters.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).OrderBy(item => item.Key).ToArrayAsync(cancellationToken);

    public async Task AddParametersAsync(IEnumerable<OrganizationParameter> parameters, CancellationToken cancellationToken)
        => await appContext.OrganizationParameters.AddRangeAsync(parameters, cancellationToken);

    public async Task<IReadOnlyCollection<PaymentMethod>> GetPaymentMethodsAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken)
        => await appContext.PaymentMethods.AsNoTracking().IgnoreQueryFilters().Where(item => item.TenantId == tenantId && (includeInactive || item.IsActive))
            .OrderBy(item => item.Name).ToArrayAsync(cancellationToken);

    public Task<PaymentMethod?> GetPaymentMethodAsync(Guid tenantId, Guid paymentMethodId, CancellationToken cancellationToken)
        => appContext.PaymentMethods.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == paymentMethodId, cancellationToken);

    public Task<bool> PaymentMethodNameExistsAsync(Guid tenantId, string normalizedName, Guid? excludedId, CancellationToken cancellationToken)
        => appContext.PaymentMethods.IgnoreQueryFilters().AnyAsync(item => item.TenantId == tenantId && item.NormalizedName == normalizedName && item.Id != excludedId, cancellationToken);

    public async Task AddPaymentMethodAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken)
        => await appContext.PaymentMethods.AddAsync(paymentMethod, cancellationToken);

    public void RemovePaymentMethod(PaymentMethod paymentMethod) => appContext.PaymentMethods.Remove(paymentMethod);

    public async Task EnableAllPermissionsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var ids = await platformContext.Permissions.AsNoTracking().Select(permission => permission.Id).ToArrayAsync(cancellationToken);
        await platformContext.TenantPermissions.AddRangeAsync(ids.Select(id => new TenantPermission { TenantId = tenantId, PermissionId = id }), cancellationToken);
    }

    public async Task<IReadOnlyCollection<EnabledModulePermissionsDto>> GetEnabledPermissionCatalogAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var rows = await platformContext.TenantPermissions.AsNoTracking().Where(item => item.TenantId == tenantId && item.Permission.Module.IsActive
                && item.Permission.Code != "settings.manage")
            .Select(item => new
            {
                ModuleId = item.Permission.ModuleId,
                ModuleCode = item.Permission.Module.Code,
                ModuleName = item.Permission.Module.Name,
                PermissionId = item.PermissionId,
                PermissionCode = item.Permission.Code,
                PermissionName = item.Permission.Description
            })
            .OrderBy(item => item.ModuleCode).ThenBy(item => item.PermissionCode).ToArrayAsync(cancellationToken);

        return rows.GroupBy(item => new { item.ModuleId, item.ModuleCode, item.ModuleName })
            .Select(group => new EnabledModulePermissionsDto(group.Key.ModuleId, group.Key.ModuleCode, group.Key.ModuleName,
                group.Select(item => new EnabledPermissionDto(item.PermissionId, item.PermissionCode, item.PermissionName)).ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<string>> GetEnabledPermissionCodesAsync(Guid tenantId, CancellationToken cancellationToken)
        => await platformContext.TenantPermissions.AsNoTracking().Where(item => item.TenantId == tenantId && item.Permission.Code != "settings.manage")
            .Select(item => item.Permission.Code).OrderBy(code => code).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Role>> GetRolesAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken)
        => await appContext.Roles.AsNoTracking().IgnoreQueryFilters().Include(role => role.RolePermissions)
            .Where(role => role.TenantId == tenantId && (includeInactive || role.IsActive)).OrderByDescending(role => role.IsSystem).ThenBy(role => role.Name)
            .ToArrayAsync(cancellationToken);

    public Task<Role?> GetRoleForUpdateAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
        => appContext.Roles.IgnoreQueryFilters().Include(role => role.RolePermissions)
            .SingleOrDefaultAsync(role => role.TenantId == tenantId && role.Id == roleId, cancellationToken);

    public Task<bool> RoleCodeOrNameExistsAsync(Guid tenantId, string code, string name, Guid? excludedId, CancellationToken cancellationToken)
        => appContext.Roles.IgnoreQueryFilters().AnyAsync(role => role.TenantId == tenantId && role.Id != excludedId && (role.Code == code || role.Name.ToUpper() == name.ToUpper()), cancellationToken);

    public async Task AddRoleAsync(Role role, CancellationToken cancellationToken) => await appContext.Roles.AddAsync(role, cancellationToken);

    public async Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken)
    {
        var current = await appContext.RolePermissions.IgnoreQueryFilters().Where(item => item.RoleId == roleId).ToArrayAsync(cancellationToken);
        appContext.RolePermissions.RemoveRange(current);
        var ids = await platformContext.Permissions.AsNoTracking().Where(permission => permissionCodes.Contains(permission.Code))
            .Select(permission => permission.Id).ToArrayAsync(cancellationToken);
        await appContext.RolePermissions.AddRangeAsync(ids.Select(id => new RolePermission { RoleId = roleId, PermissionId = id }), cancellationToken);
    }

    public async Task<bool> RoleIsInUseAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
        => await platformContext.TenantMemberships.AnyAsync(item => item.TenantId == tenantId && item.RoleId == roleId, cancellationToken)
            || await platformContext.TenantInvitations.AnyAsync(item => item.TenantId == tenantId && item.RoleId == roleId, cancellationToken);

    public void RemoveRole(Role role)
    {
        appContext.RolePermissions.RemoveRange(role.RolePermissions);
        appContext.Roles.Remove(role);
    }

    public async Task<IReadOnlyCollection<TenantMembership>> GetMembershipsAsync(Guid tenantId, CancellationToken cancellationToken)
        => await platformContext.TenantMemberships.AsNoTracking().Include(item => item.User)
            .Where(item => item.TenantId == tenantId).OrderBy(item => item.User.Email).ToArrayAsync(cancellationToken);

    public Task<TenantMembership?> GetMembershipForUpdateAsync(Guid tenantId, Guid membershipId, CancellationToken cancellationToken)
        => platformContext.TenantMemberships.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == membershipId, cancellationToken);

    public async Task<bool> HasAnotherActiveOwnerAsync(Guid tenantId, Guid excludedMembershipId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var ownerRoleIds = await appContext.Roles.AsNoTracking().IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId && r.Code == "TENANT_OWNER" && r.IsActive)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (ownerRoleIds.Count == 0) return false;

        return await platformContext.TenantMemberships.AnyAsync(item => item.TenantId == tenantId && item.Id != excludedMembershipId
            && ownerRoleIds.Contains(item.RoleId) && (item.IsActive || item.DisabledUntil <= now), cancellationToken);
    }

    public void RemoveMembership(TenantMembership membership) => platformContext.TenantMemberships.Remove(membership);

    public async Task<IReadOnlyCollection<TenantInvitation>> GetInvitationsAsync(Guid tenantId, CancellationToken cancellationToken)
        => await platformContext.TenantInvitations.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.AcceptedAt == null && item.RevokedAt == null)
            .OrderBy(item => item.Email).ToArrayAsync(cancellationToken);

    public Task<TenantInvitation?> GetInvitationAsync(Guid tenantId, Guid invitationId, CancellationToken cancellationToken)
        => platformContext.TenantInvitations.SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == invitationId, cancellationToken);

    public Task<TenantInvitation?> GetInvitationByEmailAsync(Guid tenantId, string normalizedEmail, CancellationToken cancellationToken)
        => platformContext.TenantInvitations
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyCollection<TenantInvitation>> GetPendingInvitationsAsync(string normalizedEmail, CancellationToken cancellationToken)
        => await platformContext.TenantInvitations.Include(item => item.Tenant)
            .Where(item => item.NormalizedEmail == normalizedEmail && item.AcceptedAt == null && item.RevokedAt == null && item.Tenant.IsActive)
            .ToArrayAsync(cancellationToken);

    public async Task AddInvitationAsync(TenantInvitation invitation, CancellationToken cancellationToken)
        => await platformContext.TenantInvitations.AddAsync(invitation, cancellationToken);

    public void RemoveInvitation(TenantInvitation invitation) => platformContext.TenantInvitations.Remove(invitation);
}
