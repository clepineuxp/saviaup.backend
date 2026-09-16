using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Core.Security;

public sealed class PermissionService(IPermissionRepository repository) : IPermissionService
{
    public Task<bool> IsAllowedAsync(Guid tenantId, Guid roleId, string permissionCode, CancellationToken cancellationToken)
        => repository.RoleHasPermissionAsync(tenantId, roleId, permissionCode, cancellationToken);
}
