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

public sealed record ProductRecipeItemDto(
    Guid Id,
    Guid? IngredientId,
    string? IngredientName,
    string? MeasurementUnitName,
    string? MeasurementUnitCode,
    string? CustomIngredientName,
    decimal Quantity,
    string? Notes,
    int Order,
    bool IsLinked);

public sealed record ProductRecipeItemRequest(
    Guid? IngredientId,
    [MaxLength(160)] string? CustomIngredientName,
    [Range(typeof(decimal), "0.0001", "9999999999999999.9999")] decimal Quantity,
    [MaxLength(500)] string? Notes,
    int Order = 0);

public sealed record ProductVariationDto(
    Guid Id,
    string Name,
    decimal SalePrice,
    int Order,
    bool IsActive);

public sealed record ProductVariationRequest(
    Guid? Id,
    [Required, MaxLength(120)] string Name,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal SalePrice,
    int Order = 0,
    bool IsActive = true);

public sealed record ProductComboOptionDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int ProductQuantity,
    decimal PriceAdjustment,
    int Order,
    Guid? ProductVariationId,
    string? ProductVariationName);

public sealed record ProductComboGroupDto(
    Guid Id,
    string Name,
    string SelectionType,
    bool IsRequired,
    int MinSelections,
    int MaxSelections,
    int Order,
    IReadOnlyCollection<ProductComboOptionDto> Options);

public sealed record ProductComboOptionRequest(
    Guid ProductId,
    [Range(1, 1000)] int ProductQuantity,
    [Range(typeof(decimal), "-9999999999999999.99", "9999999999999999.99")] decimal PriceAdjustment = 0,
    int Order = 0,
    Guid? ProductVariationId = null);

public sealed record ProductComboGroupRequest(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(10)] string SelectionType,
    bool IsRequired,
    [Range(0, 1000)] int MinSelections,
    [Range(1, 1000)] int MaxSelections,
    [Required, MinLength(1)] IReadOnlyCollection<ProductComboOptionRequest> Options,
    int Order = 0);

public sealed record ProductDto(
    Guid Id,
    string Type,
    string Name,
    string? Description,
    string? Image,
    CategoryReferenceDto Category,
    decimal? SalePrice,
    int? PreparationTimeMinutes,
    bool IsInventoryTracked,
    bool IsActive,
    string? CreatedByUserName,
    string? LastModifiedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ProductRecipeItemDto> Recipe,
    IReadOnlyCollection<ProductVariationDto> Variations,
    IReadOnlyCollection<ProductComboGroupDto> ComboGroups);

public sealed record CreateProductRequest(
    string? Type,
    [Required, MaxLength(120)] string Name,
    Guid CategoryId,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal? SalePrice,
    [MaxLength(1000)] string? Description,
    string? Image,
    [Range(0, int.MaxValue)] int? PreparationTimeMinutes,
    bool IsInventoryTracked,
    IReadOnlyCollection<ProductRecipeItemRequest>? Recipe = null,
    IReadOnlyCollection<ProductVariationRequest>? Variations = null,
    IReadOnlyCollection<ProductComboGroupRequest>? ComboGroups = null);

public sealed record UpdateProductRequest(
    string? Type,
    [Required, MaxLength(120)] string Name,
    Guid CategoryId,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal? SalePrice,
    [MaxLength(1000)] string? Description,
    string? Image,
    [Range(0, int.MaxValue)] int? PreparationTimeMinutes,
    bool IsInventoryTracked,
    IReadOnlyCollection<ProductRecipeItemRequest>? Recipe = null,
    IReadOnlyCollection<ProductVariationRequest>? Variations = null,
    IReadOnlyCollection<ProductComboGroupRequest>? ComboGroups = null);

public sealed record SetProductStatusRequest(bool IsActive);
