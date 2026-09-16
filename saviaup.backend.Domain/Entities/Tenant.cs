namespace SaviaUp.Backend.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ResponsibleName { get; set; }
    public string? Document { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public byte[]? LogoData { get; set; }
    public string? LogoContentType { get; set; }
    public string? LogoFileName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresOpenCashRegister { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<TenantPermission> EnabledPermissions { get; set; } = [];
    public ICollection<TenantInvitation> Invitations { get; set; } = [];
}
