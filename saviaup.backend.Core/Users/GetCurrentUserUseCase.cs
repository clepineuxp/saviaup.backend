using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Users;

public sealed class GetCurrentUserUseCase(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IDateTimeProvider clock) : IGetCurrentUserUseCase
{
    public async Task<Result<UserDto>> ExecuteAsync(
        Guid userId,
        Guid? tenantId,
        Guid? roleId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return Result<UserDto>.Failure(Errors.AccountDisabled);

        ActiveTenantDto? activeTenant = null;
        RoleDto? role = null;
        IReadOnlyCollection<string> permissions = [];
        if (tenantId.HasValue && roleId.HasValue)
        {
            var membership = await tenantRepository.GetMembershipAsync(userId, tenantId.Value, cancellationToken);
            var roleEntity = membership is not null ? await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken) : null;

            if (membership is not null && membership.RoleId == roleId && membership.IsEnabledAt(clock.UtcNow) && membership.Tenant.IsActive && roleEntity is not null && roleEntity.IsActive)
            {
                activeTenant = new ActiveTenantDto(membership.TenantId, membership.Tenant.Name);
                role = new RoleDto(membership.RoleId, roleEntity.Code, roleEntity.Name);
                permissions = await permissionRepository.GetForRoleAsync(membership.TenantId, membership.RoleId, cancellationToken);
            }
        }

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PreferredLanguage,
            activeTenant,
            role,
            permissions));
    }
}
