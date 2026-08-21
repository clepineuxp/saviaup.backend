using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Domain.Ports;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user, Guid sessionId, Guid? tenantId, Guid? roleId);
}

public interface ITokenGenerator
{
    string Generate();
    string Hash(string token);
}

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string language, string resetLink, CancellationToken cancellationToken);
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? SessionId { get; }
    Guid? TenantId { get; }
    Guid? RoleId { get; }
}

public interface ITableRealtimeNotifier
{
    Task StatusChangedAsync(Guid tenantId, TableStatusChangedEvent notification, CancellationToken cancellationToken);
    Task OrderUpdatedAsync(Guid tenantId, TableOrderUpdatedEvent notification, CancellationToken cancellationToken);
}
