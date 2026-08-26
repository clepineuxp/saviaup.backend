using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(
    ApplicationDbContext appContext,
    PlatformDbContext platformContext) : IRoleRepository
{
    public async Task AddAsync(Role role, CancellationToken cancellationToken)
        => await appContext.Roles.AddAsync(role, cancellationToken);

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken)
        => await appContext.Roles.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public async Task AssignAllPermissionsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var permissionIds = await platformContext.Permissions.AsNoTracking().Select(permission => permission.Id).ToArrayAsync(cancellationToken);
        await appContext.RolePermissions.AddRangeAsync(
            permissionIds.Select(permissionId => new RolePermission { RoleId = roleId, PermissionId = permissionId }),
            cancellationToken);
    }
}
