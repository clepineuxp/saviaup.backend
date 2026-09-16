namespace SaviaUp.Backend.Domain.Entities;

public sealed class TenantMembership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? DisabledUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;

    public bool IsEnabledAt(DateTimeOffset now) => IsActive || (DisabledUntil.HasValue && DisabledUntil.Value <= now);
}
