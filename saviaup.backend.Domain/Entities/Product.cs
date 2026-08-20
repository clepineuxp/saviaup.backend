namespace SaviaUp.Backend.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public ProductType Type { get; set; } = ProductType.Normal;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal SalePrice { get; set; }
    public int? PreparationTimeMinutes { get; set; }
    public bool IsInventoryTracked { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
