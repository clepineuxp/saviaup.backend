namespace SaviaUp.Backend.Domain.Entities;

public sealed class SupportAccessAudit
{
    public Guid Id { get; set; }
    public Guid SupportUserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public string? Ticket { get; set; }
    public DateTimeOffset AccessedAt { get; set; }
    public User SupportUser { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
