using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class ModuleRepository(SaviaUpDbContext context) : IModuleRepository
{
    public async Task<IReadOnlyCollection<AvailableModuleReference>> GetAvailableForRoleAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
        => await context.Modules
            .AsNoTracking()
            .Where(module => module.IsActive
                && module.Permissions.Any(permission => permission.RolePermissions.Any(rolePermission =>
                    rolePermission.RoleId == roleId
                    && rolePermission.Role.TenantId == tenantId
                    && rolePermission.Role.IsActive)
                    && permission.TenantPermissions.Any(enabled => enabled.TenantId == tenantId)))
            .OrderBy(module => module.Code)
            .Select(module => new AvailableModuleReference(module.Id, module.Code))
            .ToArrayAsync(cancellationToken);
}
