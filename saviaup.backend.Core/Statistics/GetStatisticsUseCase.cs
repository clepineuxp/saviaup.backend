using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Statistics;

public sealed class GetStatisticsUseCase(IStatisticsRepository repository) : IGetStatisticsUseCase
{
    public async Task<Result<StatisticsDashboardDto>> ExecuteAsync(
        Guid tenantId,
        string? period,
        bool? includeTips,
        CancellationToken cancellationToken)
    {
        var normalizedPeriod = string.Equals(period, "last_30_days", StringComparison.OrdinalIgnoreCase)
            ? "last_30_days"
            : "current_month";
        var shouldIncludeTips = includeTips ?? false;

        var data = await repository.GetDashboardStatisticsAsync(
            tenantId,
            normalizedPeriod,
            shouldIncludeTips,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Result<StatisticsDashboardDto>.Success(data);
    }
}
