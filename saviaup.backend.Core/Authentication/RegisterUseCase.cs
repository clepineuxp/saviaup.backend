using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Security;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Authentication;

public sealed class RegisterUseCase(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ISettingsRepository settingsRepository,
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
        var invitations = await settingsRepository.GetPendingInvitationsAsync(normalizedEmail, cancellationToken);
        foreach (var invitation in invitations)
        {
            await tenantRepository.AddMembershipAsync(new TenantMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                TenantId = invitation.TenantId,
                Tenant = invitation.Tenant,
                RoleId = invitation.RoleId,
                Role = invitation.Role,
                IsActive = true,
                CreatedAt = now
            }, cancellationToken);
            invitation.AcceptedAt = now;
        }
        var issued = await sessionIssuer.IssueAsync(user, Guid.NewGuid(), null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RegisterResponse>.Success(new RegisterResponse(issued.Session, "tenant-selection"));
    }
}
