namespace SaviaUp.Backend.Domain.Entities;

public sealed class ProductComboOption
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ComboGroupId { get; set; }
    public Guid ProductId { get; set; }
    public int ProductQuantity { get; set; } = 1;
    public decimal PriceAdjustment { get; set; }
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ProductComboGroup ComboGroup { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
