namespace SaviaUp.Backend.Domain.Entities;

public sealed class TenantInvitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public User InvitedByUser { get; set; } = null!;
}
