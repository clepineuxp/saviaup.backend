namespace SaviaUp.Backend.Domain.Ports;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
}
