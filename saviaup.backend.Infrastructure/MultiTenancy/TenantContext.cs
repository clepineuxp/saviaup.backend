using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.MultiTenancy;

public sealed class TenantContext(
    ICurrentUserContext currentUserContext,
    IPrintAgentContext printAgentContext) : ITenantContext
{
    public Guid TenantId => currentUserContext.TenantId ?? printAgentContext.TenantId ?? Guid.Empty;
    public bool HasTenant => TenantId != Guid.Empty;
}
