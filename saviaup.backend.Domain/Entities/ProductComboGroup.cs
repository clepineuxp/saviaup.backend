namespace SaviaUp.Backend.Domain.Entities;

public sealed class ProductComboGroup
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ComboProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProductComboSelectionType SelectionType { get; set; }
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Product ComboProduct { get; set; } = null!;
    public ICollection<ProductComboOption> Options { get; set; } = [];
}
