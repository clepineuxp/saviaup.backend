namespace SaviaUp.Backend.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? TableId { get; set; }
    public Guid? CashRegisterShiftId { get; set; }
    public int OrderNumber { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, PAID, CANCELLED
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentDetailsJson { get; set; }
    public string? Observations { get; set; }

    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public Guid? LastModifiedByUserId { get; set; }
    public string? LastModifiedByUserName { get; set; }
    public Guid? PaidByUserId { get; set; }
    public string? PaidByUserName { get; set; }

    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public RestaurantTable? Table { get; set; }
    public CashRegisterShift? CashRegisterShift { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderReceipt> Receipts { get; set; } = new List<OrderReceipt>();
}
