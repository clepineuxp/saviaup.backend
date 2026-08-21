namespace SaviaUp.Backend.Domain.Entities;

public sealed class TenantPermission
{
    public Guid TenantId { get; set; }
    public Guid PermissionId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
