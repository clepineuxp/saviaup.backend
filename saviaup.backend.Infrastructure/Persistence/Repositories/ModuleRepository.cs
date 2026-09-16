using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class ModuleRepository(
    PlatformDbContext platformContext,
    ApplicationDbContext appContext) : IModuleRepository
{
    public async Task<IReadOnlyCollection<AvailableModuleReference>> GetAvailableForRoleAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var rolePermissionIds = await appContext.RolePermissions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        if (rolePermissionIds.Count == 0) return [];

        var tenantPermissionIds = await platformContext.TenantPermissions
            .AsNoTracking()
            .Where(tp => tp.TenantId == tenantId)
            .Select(tp => tp.PermissionId)
            .ToListAsync(cancellationToken);

        var activePermissionIds = rolePermissionIds.Intersect(tenantPermissionIds).ToList();
        if (activePermissionIds.Count == 0) return [];

        return await platformContext.Modules
            .AsNoTracking()
            .Where(module => module.IsActive && module.Permissions.Any(p => activePermissionIds.Contains(p.Id)))
            .OrderBy(module => module.Code)
            .Select(module => new AvailableModuleReference(module.Id, module.Code))
            .ToArrayAsync(cancellationToken);
    }
}
