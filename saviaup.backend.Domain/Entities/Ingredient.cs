namespace SaviaUp.Backend.Domain.Entities;

public sealed class Ingredient
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid MeasurementUnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal CurrentStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public MeasurementUnit MeasurementUnit { get; set; } = null!;
    public ICollection<InventoryMovement> Movements { get; set; } = [];
}
