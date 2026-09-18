using Moq;
using SaviaUp.Backend.Core.Statistics;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Core.Tests;

public sealed class StatisticsUseCaseTests
{
    [Fact]
    public async Task CustomRange_WithoutBothDates_ReturnsValidationError()
    {
        var repository = new Mock<IStatisticsRepository>();
        var useCase = new GetStatisticsUseCase(repository.Object, new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            "custom_range",
            new DateOnly(2026, 9, 1),
            null,
            false,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.Verify(
            x => x.GetDashboardStatisticsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<bool>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CustomRange_WithValidDates_PassesLocalDatesToRepository()
    {
        var tenantId = Guid.NewGuid();
        var fromDate = new DateOnly(2026, 9, 1);
        var toDate = new DateOnly(2026, 9, 15);
        var repository = new Mock<IStatisticsRepository>();
        repository.Setup(x => x.GetDashboardStatisticsAsync(
                tenantId,
                "custom_range",
                fromDate,
                toDate,
                true,
                TestSupport.Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyDashboard());
        var useCase = new GetStatisticsUseCase(repository.Object, new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(
            tenantId,
            "custom_range",
            fromDate,
            toDate,
            true,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(
            x => x.GetDashboardStatisticsAsync(
                tenantId,
                "custom_range",
                fromDate,
                toDate,
                true,
                TestSupport.Now,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static StatisticsDashboardDto EmptyDashboard() => new(
        "custom_range",
        true,
        new SummaryStatisticsDto(0, 0, 0, 0, 0, null),
        [],
        [],
        [],
        [],
        []);
}
