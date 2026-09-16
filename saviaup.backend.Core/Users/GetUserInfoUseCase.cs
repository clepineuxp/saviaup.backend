using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Users;

public sealed class GetUserInfoUseCase(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IDateTimeProvider clock) : IGetUserInfoUseCase
{
    public async Task<Result<UserInfoDto>> ExecuteAsync(
        Guid userId,
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return Result<UserInfoDto>.Failure(Errors.AccountDisabled);

        var membership = await tenantRepository.GetMembershipAsync(userId, tenantId, cancellationToken);
        var role = membership is not null ? await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken) : null;

        if (membership is null
            || !membership.IsEnabledAt(clock.UtcNow)
            || !membership.Tenant.IsActive
            || role is null
            || !role.IsActive
            || membership.RoleId != roleId)
        {
            return Result<UserInfoDto>.Failure(Errors.TenantAccessDenied);
        }

        return Result<UserInfoDto>.Success(new UserInfoDto(
            user.FirstName,
            user.LastName,
            new ActiveTenantDto(membership.TenantId, membership.Tenant.Name),
            new RoleDto(membership.RoleId, role.Code, role.Name)));
    }
}
