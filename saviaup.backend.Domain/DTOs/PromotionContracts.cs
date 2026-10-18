using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record PromotionTargetRequest(
    string TargetType,
    Guid? ProductId = null,
    Guid? ProductVariationId = null,
    Guid? CategoryId = null);

public sealed record PromotionTargetDto(
    string TargetType,
    Guid? ProductId,
    string? ProductName,
    Guid? ProductVariationId,
    string? ProductVariationName,
    Guid? CategoryId,
    string? CategoryName);

public sealed record CreatePromotionRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    [Required, MaxLength(20)] string Type,
    [Required, MaxLength(20)] string ValueType,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Value,
    [Range(2, 10000)] int? RequiredQuantity,
    [Range(1, 10000)] int? DiscountedQuantity,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateOnly? SpecificDate,
    [Range(1, 12)] int? MonthOfYear,
    [Range(0, 127)] int WeekDaysMask,
    [Required, MinLength(1)] IReadOnlyCollection<PromotionTargetRequest> Targets,
    bool IsActive = true);

public sealed record UpdatePromotionRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    [Required, MaxLength(20)] string Type,
    [Required, MaxLength(20)] string ValueType,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Value,
    [Range(2, 10000)] int? RequiredQuantity,
    [Range(1, 10000)] int? DiscountedQuantity,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateOnly? SpecificDate,
    [Range(1, 12)] int? MonthOfYear,
    [Range(0, 127)] int WeekDaysMask,
    [Required, MinLength(1)] IReadOnlyCollection<PromotionTargetRequest> Targets,
    bool IsActive = true);

public sealed record SetPromotionStatusRequest(bool IsActive);

public sealed record PromotionDto(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    string ValueType,
    decimal Value,
    int? RequiredQuantity,
    int? DiscountedQuantity,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateOnly? SpecificDate,
    int? MonthOfYear,
    int WeekDaysMask,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<PromotionTargetDto> Targets);

public sealed record ProductPromotionDto(
    Guid Id,
    string Name,
    string Type,
    string ValueType,
    decimal Value,
    decimal OriginalUnitPrice,
    decimal FinalUnitPrice,
    decimal DiscountAmount,
    int? RequiredQuantity = null,
    int? DiscountedQuantity = null);
