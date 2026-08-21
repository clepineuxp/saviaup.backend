namespace SaviaUp.Backend.Domain.Entities;

public sealed class DiningArea
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ICollection<RestaurantTable> Tables { get; set; } = [];
}
