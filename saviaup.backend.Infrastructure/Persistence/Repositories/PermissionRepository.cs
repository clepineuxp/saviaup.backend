using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository(
    PlatformDbContext platformContext,
    ApplicationDbContext appContext) : IPermissionRepository
{
    public async Task<bool> RoleHasPermissionAsync(Guid tenantId, Guid roleId, string permissionCode, CancellationToken cancellationToken)
    {
        var activePermissions = await GetForRoleAsync(tenantId, roleId, cancellationToken);
        return activePermissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyCollection<string>> GetForRoleAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
    {
        var rolePermissionIds = await appContext.RolePermissions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        if (rolePermissionIds.Count == 0) return [];

        var enabledPermissionIds = await platformContext.TenantPermissions
            .AsNoTracking()
            .Where(tp => tp.TenantId == tenantId)
            .Select(tp => tp.PermissionId)
            .ToListAsync(cancellationToken);

        var activePermissionIds = rolePermissionIds.Intersect(enabledPermissionIds).ToList();
        if (activePermissionIds.Count == 0) return [];

        return await platformContext.Permissions
            .AsNoTracking()
            .Where(p => activePermissionIds.Contains(p.Id))
            .OrderBy(p => p.Code)
            .Select(p => p.Code)
            .ToArrayAsync(cancellationToken);
    }
}
