namespace SaviaUp.Backend.Domain.Entities;

public sealed class ProductVariation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
