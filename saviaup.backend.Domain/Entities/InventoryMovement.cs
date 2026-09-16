namespace SaviaUp.Backend.Domain.Entities;

public sealed class InventoryMovement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid IngredientId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
}
