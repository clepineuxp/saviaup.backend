using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class SettingsRepository(SaviaUpDbContext context) : ISettingsRepository
{
    public Task<Tenant?> GetTenantForUpdateAsync(Guid tenantId, CancellationToken cancellationToken)
        => context.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);

    public async Task<IReadOnlyCollection<OrganizationParameter>> GetParametersAsync(Guid tenantId, CancellationToken cancellationToken)
        => await context.OrganizationParameters.Where(item => item.TenantId == tenantId).OrderBy(item => item.Key).ToArrayAsync(cancellationToken);

    public async Task AddParametersAsync(IEnumerable<OrganizationParameter> parameters, CancellationToken cancellationToken)
        => await context.OrganizationParameters.AddRangeAsync(parameters, cancellationToken);

    public async Task<IReadOnlyCollection<PaymentMethod>> GetPaymentMethodsAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken)
        => await context.PaymentMethods.AsNoTracking().Where(item => item.TenantId == tenantId && (includeInactive || item.IsActive))
            .OrderBy(item => item.Name).ToArrayAsync(cancellationToken);

    public Task<PaymentMethod?> GetPaymentMethodAsync(Guid tenantId, Guid paymentMethodId, CancellationToken cancellationToken)
        => context.PaymentMethods.SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == paymentMethodId, cancellationToken);

    public Task<bool> PaymentMethodNameExistsAsync(Guid tenantId, string normalizedName, Guid? excludedId, CancellationToken cancellationToken)
        => context.PaymentMethods.AnyAsync(item => item.TenantId == tenantId && item.NormalizedName == normalizedName && item.Id != excludedId, cancellationToken);

    public async Task AddPaymentMethodAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken)
        => await context.PaymentMethods.AddAsync(paymentMethod, cancellationToken);

    public void RemovePaymentMethod(PaymentMethod paymentMethod) => context.PaymentMethods.Remove(paymentMethod);

    public async Task EnableAllPermissionsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var ids = await context.Permissions.AsNoTracking().Select(permission => permission.Id).ToArrayAsync(cancellationToken);
        await context.TenantPermissions.AddRangeAsync(ids.Select(id => new TenantPermission { TenantId = tenantId, PermissionId = id }), cancellationToken);
    }

    public async Task<IReadOnlyCollection<EnabledModulePermissionsDto>> GetEnabledPermissionCatalogAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var rows = await context.TenantPermissions.AsNoTracking().Where(item => item.TenantId == tenantId && item.Permission.Module.IsActive
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
        => await context.TenantPermissions.AsNoTracking().Where(item => item.TenantId == tenantId && item.Permission.Code != "settings.manage")
            .Select(item => item.Permission.Code).OrderBy(code => code).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Role>> GetRolesAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken)
        => await context.Roles.AsNoTracking().Include(role => role.RolePermissions).ThenInclude(item => item.Permission)
            .Where(role => role.TenantId == tenantId && (includeInactive || role.IsActive)).OrderByDescending(role => role.IsSystem).ThenBy(role => role.Name)
            .ToArrayAsync(cancellationToken);

    public Task<Role?> GetRoleForUpdateAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
        => context.Roles.Include(role => role.RolePermissions).ThenInclude(item => item.Permission)
            .SingleOrDefaultAsync(role => role.TenantId == tenantId && role.Id == roleId, cancellationToken);

    public Task<bool> RoleCodeOrNameExistsAsync(Guid tenantId, string code, string name, Guid? excludedId, CancellationToken cancellationToken)
        => context.Roles.AnyAsync(role => role.TenantId == tenantId && role.Id != excludedId && (role.Code == code || role.Name.ToUpper() == name.ToUpper()), cancellationToken);

    public async Task AddRoleAsync(Role role, CancellationToken cancellationToken) => await context.Roles.AddAsync(role, cancellationToken);

    public async Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken)
    {
        var current = await context.RolePermissions.Where(item => item.RoleId == roleId).ToArrayAsync(cancellationToken);
        context.RolePermissions.RemoveRange(current);
        var ids = await context.Permissions.AsNoTracking().Where(permission => permissionCodes.Contains(permission.Code))
            .Select(permission => permission.Id).ToArrayAsync(cancellationToken);
        await context.RolePermissions.AddRangeAsync(ids.Select(id => new RolePermission { RoleId = roleId, PermissionId = id }), cancellationToken);
    }

    public async Task<bool> RoleIsInUseAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
        => await context.TenantMemberships.AnyAsync(item => item.TenantId == tenantId && item.RoleId == roleId, cancellationToken)
            || await context.TenantInvitations.AnyAsync(item => item.TenantId == tenantId && item.RoleId == roleId, cancellationToken);

    public void RemoveRole(Role role)
    {
        context.RolePermissions.RemoveRange(role.RolePermissions);
        context.Roles.Remove(role);
    }

    public async Task<IReadOnlyCollection<TenantMembership>> GetMembershipsAsync(Guid tenantId, CancellationToken cancellationToken)
        => await context.TenantMemberships.AsNoTracking().Include(item => item.User).Include(item => item.Role)
            .Where(item => item.TenantId == tenantId).OrderBy(item => item.User.Email).ToArrayAsync(cancellationToken);

    public Task<TenantMembership?> GetMembershipForUpdateAsync(Guid tenantId, Guid membershipId, CancellationToken cancellationToken)
        => context.TenantMemberships.Include(item => item.User).Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == membershipId, cancellationToken);

    public Task<bool> HasAnotherActiveOwnerAsync(Guid tenantId, Guid excludedMembershipId, DateTimeOffset now, CancellationToken cancellationToken)
        => context.TenantMemberships.AnyAsync(item => item.TenantId == tenantId && item.Id != excludedMembershipId
            && item.Role.Code == "TENANT_OWNER" && item.Role.IsActive && (item.IsActive || item.DisabledUntil <= now), cancellationToken);

    public void RemoveMembership(TenantMembership membership) => context.TenantMemberships.Remove(membership);

    public async Task<IReadOnlyCollection<TenantInvitation>> GetInvitationsAsync(Guid tenantId, CancellationToken cancellationToken)
        => await context.TenantInvitations.AsNoTracking().Include(item => item.Role)
            .Where(item => item.TenantId == tenantId && item.AcceptedAt == null && item.RevokedAt == null)
            .OrderBy(item => item.Email).ToArrayAsync(cancellationToken);

    public Task<TenantInvitation?> GetInvitationAsync(Guid tenantId, Guid invitationId, CancellationToken cancellationToken)
        => context.TenantInvitations.Include(item => item.Role).SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == invitationId, cancellationToken);

    public Task<TenantInvitation?> GetInvitationByEmailAsync(Guid tenantId, string normalizedEmail, CancellationToken cancellationToken)
        => context.TenantInvitations.Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyCollection<TenantInvitation>> GetPendingInvitationsAsync(string normalizedEmail, CancellationToken cancellationToken)
        => await context.TenantInvitations.Include(item => item.Tenant).Include(item => item.Role)
            .Where(item => item.NormalizedEmail == normalizedEmail && item.AcceptedAt == null && item.RevokedAt == null && item.Tenant.IsActive && item.Role.IsActive)
            .ToArrayAsync(cancellationToken);

    public async Task AddInvitationAsync(TenantInvitation invitation, CancellationToken cancellationToken)
        => await context.TenantInvitations.AddAsync(invitation, cancellationToken);

    public void RemoveInvitation(TenantInvitation invitation) => context.TenantInvitations.Remove(invitation);
}
