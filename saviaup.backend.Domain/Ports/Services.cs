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
    Task SendOrganizationInvitationAsync(string email, string language, string organizationName, string invitationLink, CancellationToken cancellationToken);
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? UserDisplayName { get; }
    Guid? SessionId { get; }
    Guid? TenantId { get; }
    Guid? RoleId { get; }
}

public interface ITableRealtimeNotifier
{
    Task StatusChangedAsync(Guid tenantId, TableStatusChangedEvent notification, CancellationToken cancellationToken);
    Task OrderUpdatedAsync(Guid tenantId, TableOrderUpdatedEvent notification, CancellationToken cancellationToken);
    Task SalesDataInvalidatedAsync(Guid tenantId, TableSalesDataInvalidatedEvent notification, CancellationToken cancellationToken);
}

public interface IPrintAgentContext
{
    bool IsAuthenticated { get; }
    Guid? AgentId { get; }
    Guid? TenantId { get; }
    Guid? LocationId { get; }
}

public interface IPrintingRealtimeNotifier
{
    Task JobAvailableAsync(Guid agentId, Guid printJobId, CancellationToken cancellationToken);
    Task JobCancelledAsync(Guid agentId, Guid printJobId, CancellationToken cancellationToken);
    Task PrinterDiscoveryRequestedAsync(Guid agentId, CancellationToken cancellationToken);
}

public interface INetworkFingerprintService
{
    string? Compute(string? sourceIpAddress);
}
