namespace SaviaUp.Backend.Domain.Entities;

public sealed class Permission
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Module Module { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<TenantPermission> TenantPermissions { get; set; } = [];
}
