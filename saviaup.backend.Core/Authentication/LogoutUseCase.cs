using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Authentication;

public sealed class LogoutUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ILogoutUseCase
{
    public async Task<Result> ExecuteAsync(LogoutRequest request, Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.RevokeSessionAsync(userId, sessionId, dateTimeProvider.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
