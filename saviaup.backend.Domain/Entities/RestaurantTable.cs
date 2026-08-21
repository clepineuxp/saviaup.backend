namespace SaviaUp.Backend.Domain.Entities;

public sealed class RestaurantTable
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DiningAreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public TableShape Shape { get; set; } = TableShape.Square;
    public bool IsDelivery { get; set; }
    public bool IsCashRegister { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Available;
    public Guid? ActiveOrderId { get; set; }
    public decimal ActiveOrderTotal { get; set; }
    public DateTimeOffset? OccupiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DiningArea DiningArea { get; set; } = null!;
}
