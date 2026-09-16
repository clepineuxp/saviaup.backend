using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Authentication;

public sealed class LoginUseCase(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ILoginUseCase
{
    public async Task<Result<AuthSessionDto>> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByNormalizedEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
            return Result<AuthSessionDto>.Failure(Errors.InvalidCredentials);
        if (!user.IsActive) return Result<AuthSessionDto>.Failure(Errors.AccountDisabled);

        var membership = user.LastTenantId.HasValue
            ? await tenantRepository.GetMembershipAsync(user.Id, user.LastTenantId.Value, cancellationToken)
            : null;
        var role = membership is not null ? await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken) : null;
        if (membership is not null && (!membership.IsEnabledAt(dateTimeProvider.UtcNow) || !membership.Tenant.IsActive || role is null || !role.IsActive))
            membership = null;
        if (membership is null && user.LastTenantId.HasValue)
        {
            user.LastTenantId = null;
            user.UpdatedAt = dateTimeProvider.UtcNow;
        }

        var issued = await sessionIssuer.IssueAsync(user, Guid.NewGuid(), membership, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AuthSessionDto>.Success(issued.Session);
    }

    internal static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
