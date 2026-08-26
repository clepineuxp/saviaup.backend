using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed class ProductQueryRequest
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [MaxLength(120)] public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    [MaxLength(10)] public string? Type { get; init; }
    public bool IncludeInactive { get; init; }
}

public sealed record ProductDto(
    Guid Id,
    string Type,
    string Name,
    string? Description,
    string? Image,
    CategoryReferenceDto Category,
    decimal SalePrice,
    int? PreparationTimeMinutes,
    bool IsInventoryTracked,
    bool IsActive,
    string? CreatedByUserName,
    string? LastModifiedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateProductRequest(
    string? Type,
    [Required, MaxLength(120)] string Name,
    Guid CategoryId,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal SalePrice,
    [MaxLength(1000)] string? Description,
    string? Image,
    [Range(0, int.MaxValue)] int? PreparationTimeMinutes,
    bool IsInventoryTracked);

public sealed record UpdateProductRequest(
    string? Type,
    [Required, MaxLength(120)] string Name,
    Guid CategoryId,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal SalePrice,
    [MaxLength(1000)] string? Description,
    string? Image,
    [Range(0, int.MaxValue)] int? PreparationTimeMinutes,
    bool IsInventoryTracked);

public sealed record SetProductStatusRequest(bool IsActive);
