using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed class InventoryQueryRequest
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [MaxLength(120)] public string? Search { get; init; }
    public bool? BelowMinimum { get; init; }
}

public sealed class IngredientQueryRequest
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [MaxLength(120)] public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public bool IncludeInactive { get; init; }
}

public sealed class InventoryMovementQueryRequest
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    public Guid? IngredientId { get; init; }
    [MaxLength(20)] public string? Direction { get; init; }
}

public sealed class MeasurementUnitQueryRequest
{
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [MaxLength(120)] public string? Search { get; init; }
    public bool IncludeInactive { get; init; }
}

public sealed record CategoryReferenceDto(Guid Id, string Name, bool IsInventoryTracked);

public sealed record MeasurementUnitDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record IngredientDto(
    Guid Id,
    string Name,
    string? Description,
    CategoryReferenceDto Category,
    MeasurementUnitDto Unit,
    decimal MinimumStock,
    decimal CurrentStock,
    bool IsBelowMinimum,
    bool IsInventoryTracked,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record InventoryItemDto(
    Guid Id,
    string ItemType,
    string Name,
    CategoryReferenceDto Category,
    MeasurementUnitDto Unit,
    decimal CurrentStock,
    decimal MinimumStock,
    bool IsBelowMinimum);

public sealed record InventoryMovementDto(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    MeasurementUnitDto Unit,
    string Direction,
    string Reason,
    decimal Quantity,
    decimal StockBefore,
    decimal StockAfter,
    string? Note,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);

public sealed record CreateIngredientRequest(
    Guid CategoryId,
    Guid MeasurementUnitId,
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    [Range(typeof(decimal), "0", "999999999999999.999")] decimal MinimumStock = 0,
    [Range(typeof(decimal), "0", "999999999999999.999")] decimal InitialStock = 0);

public sealed record UpdateIngredientRequest(
    Guid CategoryId,
    Guid MeasurementUnitId,
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    [Range(typeof(decimal), "0", "999999999999999.999")] decimal MinimumStock = 0);

public sealed record SetIngredientStatusRequest(bool IsActive);

public sealed record CreateInventoryMovementRequest(
    Guid IngredientId,
    [Required, MaxLength(20)] string Direction,
    [Required, MaxLength(30)] string Reason,
    [Range(typeof(decimal), "0.001", "999999999999999.999")] decimal Quantity,
    [MaxLength(500)] string? Note);

public sealed record CreateMeasurementUnitRequest(
    [Required, MaxLength(20)] string Code,
    [Required, MaxLength(120)] string Name);

public sealed record UpdateMeasurementUnitRequest(
    [Required, MaxLength(20)] string Code,
    [Required, MaxLength(120)] string Name);

public sealed record SetMeasurementUnitStatusRequest(bool IsActive);
