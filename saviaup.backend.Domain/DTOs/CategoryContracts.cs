using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? Image,
    bool IsInventoryTracked,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int ProductCount = 0,
    int VariationCount = 0,
    int IngredientCount = 0);

public sealed record CategoryUsageCounts(
    Guid CategoryId,
    int ProductCount,
    int VariationCount,
    int IngredientCount);

public sealed record CreateCategoryRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    string? Image,
    bool IsInventoryTracked);

public sealed record UpdateCategoryRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    string? Image,
    bool IsInventoryTracked);

public sealed record SetCategoryStatusRequest(bool IsActive);
