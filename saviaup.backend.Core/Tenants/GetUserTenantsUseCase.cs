using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Tenants;

public sealed class GetUserTenantsUseCase(ITenantRepository tenantRepository) : IGetUserTenantsUseCase
{
    public async Task<Result<IReadOnlyCollection<TenantDto>>> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
        => Result<IReadOnlyCollection<TenantDto>>.Success(await tenantRepository.GetForUserAsync(userId, cancellationToken));
}
