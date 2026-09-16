namespace SaviaUp.Backend.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ImageRef { get; set; }
    public StoredImage? ImageStored { get; set; }
    public bool IsInventoryTracked { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<Ingredient> Ingredients { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
