using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository(SaviaUpDbContext context) : IPermissionRepository
{
    public Task<bool> RoleHasPermissionAsync(Guid tenantId, Guid roleId, string permissionCode, CancellationToken cancellationToken)
        => context.RolePermissions.AsNoTracking().AnyAsync(
            rolePermission => rolePermission.RoleId == roleId
                && rolePermission.Role.TenantId == tenantId
                && rolePermission.Role.IsActive
                && rolePermission.Permission.TenantPermissions.Any(enabled => enabled.TenantId == tenantId)
                && rolePermission.Permission.Code == permissionCode,
            cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetForRoleAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
        => await context.RolePermissions.AsNoTracking()
            .Where(rolePermission => rolePermission.RoleId == roleId
                && rolePermission.Role.TenantId == tenantId
                && rolePermission.Role.IsActive
                && rolePermission.Permission.TenantPermissions.Any(enabled => enabled.TenantId == tenantId))
            .OrderBy(rolePermission => rolePermission.Permission.Code)
            .Select(rolePermission => rolePermission.Permission.Code)
            .ToArrayAsync(cancellationToken);
}
