namespace SaviaUp.Backend.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ProductVariationId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? PromotionId { get; set; }
    public string? PromotionName { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, PAID, CANCELLED
    public string? Notes { get; set; }
    public bool IsCustomSale { get; set; }

    public string? CancellationReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancelledByUserName { get; set; }

    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public Guid? LastModifiedByUserId { get; set; }
    public string? LastModifiedByUserName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
    public ProductVariation? ProductVariation { get; set; }
    public Promotion? Promotion { get; set; }
    public ICollection<OrderItemComboSelection> ComboSelections { get; set; } = [];
}
