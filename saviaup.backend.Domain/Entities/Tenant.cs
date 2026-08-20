namespace SaviaUp.Backend.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<MeasurementUnit> MeasurementUnits { get; set; } = [];
    public ICollection<Ingredient> Ingredients { get; set; } = [];
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
}
