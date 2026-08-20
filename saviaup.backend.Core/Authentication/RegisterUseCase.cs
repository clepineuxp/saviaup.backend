using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Security;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Authentication;

public sealed class RegisterUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    PasswordPolicy passwordPolicy,
    SessionIssuer sessionIssuer,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IRegisterUseCase
{
    public async Task<Result<RegisterResponse>> ExecuteAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || !passwordPolicy.IsValid(request.Password))
            return Result<RegisterResponse>.Failure(Errors.Validation);
        var normalizedEmail = LoginUseCase.NormalizeEmail(request.Email);
        if (await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
            return Result<RegisterResponse>.Failure(Errors.EmailAlreadyExists);

        var now = dateTimeProvider.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PreferredLanguage = "es",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await userRepository.AddAsync(user, cancellationToken);
        var issued = await sessionIssuer.IssueAsync(user, Guid.NewGuid(), null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RegisterResponse>.Success(new RegisterResponse(issued.Session, "tenant-selection"));
    }
}
