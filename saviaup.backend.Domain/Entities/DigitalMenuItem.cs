namespace SaviaUp.Backend.Domain.Entities;

public sealed class DigitalMenuItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ItemType { get; set; } = string.Empty; // "CATEGORY" or "PRODUCT"
    public Guid TargetId { get; set; }
    public Guid? CategoryId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public string? LastModifiedByUserName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
