using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Authentication;

public sealed class RefreshTokenUseCase(
    ITokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    SessionIssuer sessionIssuer,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IRefreshTokenUseCase
{
    public async Task<Result<TokenResponse>> ExecuteAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return Result<TokenResponse>.Failure(Errors.RefreshInvalid);
        var existing = await refreshTokenRepository.GetByHashAsync(tokenGenerator.Hash(request.RefreshToken), cancellationToken);
        if (existing is null || existing.RevokedAt.HasValue) return Result<TokenResponse>.Failure(Errors.RefreshInvalid);
        var now = dateTimeProvider.UtcNow;
        if (existing.ExpiresAt <= now) return Result<TokenResponse>.Failure(Errors.RefreshExpired);
        var user = await userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || !user.IsActive) return Result<TokenResponse>.Failure(Errors.RefreshInvalid);

        TenantMembership? membership = null;
        if (existing.TenantId.HasValue)
        {
            membership = await tenantRepository.GetMembershipAsync(user.Id, existing.TenantId.Value, cancellationToken);
            var role = membership is not null ? await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken) : null;
            if (membership is null || !membership.IsEnabledAt(now) || !membership.Tenant.IsActive || role is null || !role.IsActive || membership.RoleId != existing.RoleId)
                return Result<TokenResponse>.Failure(Errors.RefreshInvalid);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            if (!await refreshTokenRepository.TryRevokeAsync(existing.Id, now, transactionToken))
                return Result<TokenResponse>.Failure(Errors.RefreshInvalid);
            var issued = await sessionIssuer.IssueAsync(user, existing.SessionId, membership, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            await refreshTokenRepository.SetReplacementAsync(existing.Id, issued.RefreshTokenId, transactionToken);
            return Result<TokenResponse>.Success(SessionIssuer.ToTokenResponse(issued.Session));
        }, cancellationToken);
    }
}
