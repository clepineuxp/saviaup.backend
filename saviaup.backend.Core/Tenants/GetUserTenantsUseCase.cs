using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Tenants;

public sealed class GetUserTenantsUseCase(IUserRepository userRepository, ITenantRepository tenantRepository) : IGetUserTenantsUseCase
{
    public async Task<Result<IReadOnlyCollection<TenantDto>>> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return Result<IReadOnlyCollection<TenantDto>>.Failure(Errors.AccountDisabled);
        var tenants = user.IsSupportUser
            ? await tenantRepository.GetForSupportAsync(cancellationToken)
            : await tenantRepository.GetForUserAsync(userId, cancellationToken);
        return Result<IReadOnlyCollection<TenantDto>>.Success(tenants);
    }
}
