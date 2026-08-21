namespace SaviaUp.Backend.Domain.Entities;

public sealed class CashRegisterShift
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public User OpenedByUser { get; set; } = null!;
}
