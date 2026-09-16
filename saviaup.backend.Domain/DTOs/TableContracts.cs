using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record DiningAreaDto(
    Guid Id,
    string Name,
    int Order,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RestaurantTableDto(
    Guid Id,
    Guid DiningAreaId,
    string Name,
    int Capacity,
    decimal PositionX,
    decimal PositionY,
    string Shape,
    bool IsDelivery,
    bool IsCashRegister,
    string Status,
    Guid? ActiveOrderId,
    decimal ActiveOrderTotal,
    DateTimeOffset? OccupiedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DiningAreaTablesDto(
    DiningAreaDto Area,
    IReadOnlyCollection<RestaurantTableDto> Tables);

public sealed record TableMetricsDto(
    int Available,
    int Occupied,
    decimal ActiveSalesTotal,
    decimal TodaySalesTotal = 0,
    decimal TodayExpensesTotal = 0,
    decimal OpenShiftSalesTotal = 0,
    decimal OpenShiftExpensesTotal = 0);

public sealed record CashRegisterGateDto(bool RequiresOpenShift, bool HasOpenShift, bool IsInteractionBlocked);

public sealed record TableOperationSnapshotDto(
    IReadOnlyCollection<DiningAreaTablesDto> Areas,
    TableMetricsDto Metrics,
    CashRegisterGateDto CashRegister);

public sealed record CreateDiningAreaRequest(
    [Required, MaxLength(120)] string Name,
    [Range(0, int.MaxValue)] int Order = 0,
    bool IsActive = true);

public sealed record UpdateDiningAreaRequest(
    [Required, MaxLength(120)] string Name,
    [Range(1, int.MaxValue)] int Order,
    bool IsActive);

public sealed record ReorderDiningAreasRequest([Required] IReadOnlyCollection<Guid> AreaIds);

public sealed record CreateRestaurantTableRequest(
    Guid DiningAreaId,
    [Required, MaxLength(120)] string Name,
    [Range(1, 100)] int Capacity,
    [Range(typeof(decimal), "-100000", "100000")] decimal PositionX,
    [Range(typeof(decimal), "-100000", "100000")] decimal PositionY,
    bool IsDelivery,
    bool IsCashRegister,
    [MaxLength(20)] string? Status = null,
    [MaxLength(30)] string? Shape = null);

public sealed record UpdateRestaurantTableRequest(
    Guid DiningAreaId,
    [Required, MaxLength(120)] string Name,
    [Range(1, 100)] int Capacity,
    [Range(typeof(decimal), "-100000", "100000")] decimal PositionX,
    [Range(typeof(decimal), "-100000", "100000")] decimal PositionY,
    bool IsDelivery,
    bool IsCashRegister,
    [Required, MaxLength(20)] string Status,
    [MaxLength(30)] string? Shape = null);

public sealed record SetTableOperationRequest(
    [Required, MaxLength(20)] string Status,
    Guid? ActiveOrderId = null,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal ActiveOrderTotal = 0);

public sealed record UpdateTableOrderRequest(
    Guid ActiveOrderId,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal Total);

public sealed record TableStatusChangedEvent(RestaurantTableDto Table, bool IsDeleted = false);
public sealed record TableOrderUpdatedEvent(Guid TableId, Guid ActiveOrderId, decimal Total, DateTimeOffset UpdatedAt);
