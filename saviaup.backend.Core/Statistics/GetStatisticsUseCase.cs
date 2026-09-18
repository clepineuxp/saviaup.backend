using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Statistics;

public sealed class GetStatisticsUseCase(IStatisticsRepository repository, IDateTimeProvider clock) : IGetStatisticsUseCase
{
    public async Task<Result<StatisticsDashboardDto>> ExecuteAsync(
        Guid tenantId,
        string? period,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? includeTips,
        CancellationToken cancellationToken)
    {
        var isCustomRange = string.Equals(period, "custom_range", StringComparison.OrdinalIgnoreCase);
        if (isCustomRange &&
            (fromDate is null || toDate is null || fromDate > toDate || toDate == DateOnly.MaxValue))
            return Result<StatisticsDashboardDto>.Failure(Errors.Validation);

        var normalizedPeriod = isCustomRange
            ? "custom_range"
            : string.Equals(period, "last_30_days", StringComparison.OrdinalIgnoreCase)
                ? "last_30_days"
                : "current_month";
        var shouldIncludeTips = includeTips ?? false;

        var data = await repository.GetDashboardStatisticsAsync(
            tenantId,
            normalizedPeriod,
            isCustomRange ? fromDate : null,
            isCustomRange ? toDate : null,
            shouldIncludeTips,
            clock.UtcNow,
            cancellationToken);

        return Result<StatisticsDashboardDto>.Success(data);
    }
}
