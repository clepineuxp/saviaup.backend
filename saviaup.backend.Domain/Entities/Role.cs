namespace SaviaUp.Backend.Domain.Entities;

public sealed class Role
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
