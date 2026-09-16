using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Security;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Authentication;

public sealed class ResetPasswordUseCase(
    IPasswordResetTokenRepository resetTokenRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ITokenGenerator tokenGenerator,
    IPasswordHasher passwordHasher,
    PasswordPolicy passwordPolicy,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IResetPasswordUseCase
{
    public async Task<Result> ExecuteAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword || !passwordPolicy.IsValid(request.NewPassword))
            return Result.Failure(Errors.Validation);
        var token = await resetTokenRepository.GetByHashAsync(tokenGenerator.Hash(request.Token), cancellationToken);
        var now = dateTimeProvider.UtcNow;
        if (token is null || token.UsedAt.HasValue || token.ExpiresAt <= now)
            return Result.Failure(Errors.PasswordResetInvalid);
        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null || !user.IsActive) return Result.Failure(Errors.PasswordResetInvalid);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            if (!await resetTokenRepository.TryMarkUsedAsync(token.Id, now, transactionToken))
                return Result.Failure(Errors.PasswordResetInvalid);
            user.PasswordHash = passwordHasher.Hash(request.NewPassword);
            user.UpdatedAt = now;
            await refreshTokenRepository.RevokeAllForUserAsync(user.Id, now, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            return Result.Success();
        }, cancellationToken);
    }
}
