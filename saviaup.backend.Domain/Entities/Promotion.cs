namespace SaviaUp.Backend.Domain.Entities;

public enum PromotionType
{
    General,
    Quantity
}

public enum PromotionValueType
{
    Percentage,
    FixedPrice
}

public enum PromotionTargetType
{
    Product,
    Variation,
    Category,
    Menu
}

public sealed class Promotion
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromotionType Type { get; set; } = PromotionType.General;
    public PromotionValueType ValueType { get; set; } = PromotionValueType.Percentage;
    public decimal Value { get; set; }
    public int? RequiredQuantity { get; set; }
    public int? DiscountedQuantity { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public DateOnly? SpecificDate { get; set; }
    public int? MonthOfYear { get; set; }
    public int WeekDaysMask { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<PromotionTarget> Targets { get; set; } = [];
}

public sealed class PromotionTarget
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PromotionId { get; set; }
    public PromotionTargetType TargetType { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ProductVariationId { get; set; }
    public Guid? CategoryId { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Product? Product { get; set; }
    public ProductVariation? ProductVariation { get; set; }
    public Category? Category { get; set; }
}
