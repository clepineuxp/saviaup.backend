using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.MultiTenancy;

public sealed class TenantContext(ICurrentUserContext currentUserContext) : ITenantContext
{
    public Guid TenantId => currentUserContext.TenantId ?? Guid.Empty;
    public bool HasTenant => currentUserContext.TenantId.HasValue && currentUserContext.TenantId.Value != Guid.Empty;
}
