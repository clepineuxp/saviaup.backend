namespace SaviaUp.Backend.Domain.Entities;

public class StoredImage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/png";
    public string Base64Content { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
