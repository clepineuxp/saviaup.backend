namespace SaviaUp.Backend.Domain.Entities;

public sealed class ProductRecipeItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? IngredientId { get; set; }
    public string? CustomIngredientName { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public Ingredient? Ingredient { get; set; }
}
