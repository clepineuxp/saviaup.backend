using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Core.Authentication;

public sealed class ForgotPasswordUseCase(
    IUserRepository userRepository,
    IPasswordResetTokenRepository resetTokenRepository,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<FrontendOptions> frontendOptions) : IForgotPasswordUseCase
{
    public async Task<Result> ExecuteAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByNormalizedEmailAsync(LoginUseCase.NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !user.IsActive) return Result.Success();

        var rawToken = tokenGenerator.Generate();
        var now = dateTimeProvider.UtcNow;
        await resetTokenRepository.AddAsync(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenGenerator.Hash(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddHours(1)
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendPasswordResetAsync(user.Email, TranslationCatalog.Normalize(request.Language ?? user.PreferredLanguage), link, cancellationToken);
        return Result.Success();
    }
}
