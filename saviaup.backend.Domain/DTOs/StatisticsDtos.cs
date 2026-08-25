namespace SaviaUp.Backend.Domain.DTOs;

public sealed record TopSellerDto(
    Guid UserId,
    string Name,
    decimal TotalSales,
    int OrdersCount);

public sealed record SummaryStatisticsDto(
    decimal TotalSales,
    int TotalOrdersCount,
    decimal AverageTicket,
    decimal TotalTips,
    TopSellerDto? TopSeller);

public sealed record DailySalesPointDto(
    string Date,
    string DayLabel,
    decimal Sales,
    decimal Tips,
    int OrdersCount);

public sealed record ProductQuantityDto(
    Guid? ProductId,
    string Name,
    int Quantity,
    decimal Percentage);

public sealed record ProductValueDto(
    Guid? ProductId,
    string Name,
    decimal Amount,
    decimal Percentage);

public sealed record MonthlyComparisonPointDto(
    int Year,
    int Month,
    string MonthLabel,
    decimal Sales);

public sealed record UserSalesSummaryDto(
    Guid UserId,
    string Name,
    decimal TotalSales,
    int OrdersCount,
    bool IsTopSeller);

public sealed record StatisticsDashboardDto(
    string Period,
    bool IncludeTips,
    SummaryStatisticsDto Summary,
    IReadOnlyCollection<DailySalesPointDto> SalesTrend,
    IReadOnlyCollection<ProductQuantityDto> TopProductsByQuantity,
    IReadOnlyCollection<ProductValueDto> TopProductsByValue,
    IReadOnlyCollection<MonthlyComparisonPointDto> MonthlyComparison,
    IReadOnlyCollection<UserSalesSummaryDto> SalesByUser);
