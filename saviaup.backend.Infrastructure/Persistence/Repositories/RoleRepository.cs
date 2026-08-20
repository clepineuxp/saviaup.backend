using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(SaviaUpDbContext context) : IRoleRepository
{
    public async Task AddAsync(Role role, CancellationToken cancellationToken)
        => await context.Roles.AddAsync(role, cancellationToken);

    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken)
        => context.Roles.AsNoTracking().SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public async Task AssignAllPermissionsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var permissionIds = await context.Permissions.AsNoTracking().Select(permission => permission.Id).ToArrayAsync(cancellationToken);
        await context.RolePermissions.AddRangeAsync(
            permissionIds.Select(permissionId => new RolePermission { RoleId = roleId, PermissionId = permissionId }),
            cancellationToken);
    }
}
