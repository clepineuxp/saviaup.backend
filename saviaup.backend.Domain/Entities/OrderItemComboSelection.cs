namespace SaviaUp.Backend.Domain.Entities;

public sealed class OrderItemComboSelection
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid? ComboGroupId { get; set; }
    public Guid? ComboOptionId { get; set; }
    public Guid? ProductId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ProductQuantity { get; set; }
    public int SelectionQuantity { get; set; }
    public decimal PriceAdjustment { get; set; }
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
    public ProductComboGroup? ComboGroup { get; set; }
    public ProductComboOption? ComboOption { get; set; }
    public Product? Product { get; set; }
}
