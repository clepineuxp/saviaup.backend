namespace SaviaUp.Backend.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long ConsecutiveNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsCashOut { get; set; }
    public string PaymentMethod { get; set; } = "Efectivo";
    
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTimeOffset ExpenseDate { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, ANNULLED

    public string? AnnulledReason { get; set; }
    public DateTimeOffset? AnnulledAt { get; set; }
    public Guid? AnnulledByUserId { get; set; }
    public string? AnnulledByUserName { get; set; }

    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public Guid LastModifiedByUserId { get; set; }
    public string LastModifiedByUserName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
