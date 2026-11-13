using Moq;
using SaviaUp.Backend.Core.Reports;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using Xunit;
using SaviaUp.Backend.Infrastructure.Time;

namespace SaviaUp.Backend.Core.Tests;

public sealed class ReportsUseCaseTests
{
    private readonly Mock<IGeneratedReportRepository> _repository = new();
    private readonly Mock<IReportJobQueue> _jobQueue = new();
    private readonly Mock<IReportGenerator> _generator = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Mock<IOrganizationTimeZone> _zone = new();
    private readonly ReportsUseCase _useCase;

    public ReportsUseCaseTests()
    {
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));
        _zone.Setup(z => z.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync("America/Bogota");
        _useCase = new ReportsUseCase(
            _repository.Object,
            _jobQueue.Object,
            _generator.Object,
            _fileStorage.Object,
            _dateTimeProvider.Object,
            _unitOfWork.Object,
            _zone.Object,
            new IanaTimeZoneService());
    }

    [Theory]
    [InlineData("America/Bogota", "2026-11-01", "2026-11-01T05:00:00Z", "2026-11-02T05:00:00Z")]
    [InlineData("America/New_York", "2026-03-08", "2026-03-08T05:00:00Z", "2026-03-09T04:00:00Z")]
    [InlineData("America/New_York", "2026-11-01", "2026-11-01T04:00:00Z", "2026-11-02T05:00:00Z")]
    public async Task RequestReportAsync_ResolvesCivilDatesAndFileNameInOrganizationZone(string zone, string date, string start, string end)
    {
        _zone.Setup(z => z.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(zone);
        var request = new CreateReportRequest("statistics", FromLocalDate: DateOnly.Parse(date), ToLocalDate: DateOnly.Parse(date), IncludeTips: true);
        GeneratedReport? captured = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<GeneratedReport>(), It.IsAny<CancellationToken>()))
            .Callback<GeneratedReport, CancellationToken>((report, _) => captured = report)
            .Returns(Task.CompletedTask);

        var result = await _useCase.RequestReportAsync(Guid.NewGuid(), Guid.NewGuid(), "User", request, default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(DateTimeOffset.Parse(start), captured.FromDate);
        Assert.Equal(DateTimeOffset.Parse(end).AddTicks(-1), captured.ToDate);
        Assert.Contains(date.Replace("-", ""), captured.FileName);
        Assert.Contains("\"IncludeTips\":true", captured.ParametersJson);
    }

    [Fact]
    public async Task RequestReportAsync_RejectsIncompleteLocalRange()
    {
        var result = await _useCase.RequestReportAsync(Guid.NewGuid(), Guid.NewGuid(), "User",
            new CreateReportRequest("statistics", FromLocalDate: new DateOnly(2026, 11, 1)), default);
        Assert.False(result.IsSuccess);
        _jobQueue.Verify(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestReportAsync_WithInvalidReportType_ReturnsValidationError()
    {
        var request = new CreateReportRequest("UNKNOWN_TYPE", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);

        var result = await _useCase.RequestReportAsync(Guid.NewGuid(), Guid.NewGuid(), "User", request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        _repository.Verify(r => r.AddAsync(It.IsAny<GeneratedReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestReportAsync_WithFromDateAfterToDate_ReturnsValidationError()
    {
        var request = new CreateReportRequest("SALES", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1));

        var result = await _useCase.RequestReportAsync(Guid.NewGuid(), Guid.NewGuid(), "User", request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error.Code);
        _repository.Verify(r => r.AddAsync(It.IsAny<GeneratedReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("SALES", "sales")]
    [InlineData("orders", "sales")]
    [InlineData("SALES_AND_EXPENSES", "sales_and_expenses")]
    [InlineData("sales_expenses", "sales_and_expenses")]
    [InlineData("STATISTICS", "statistics")]
    [InlineData("EXPENSES", "expenses")]
    [InlineData("CASH_REGISTERS", "cash_registers")]
    public async Task RequestReportAsync_NormalizesReportTypeAndEnqueues(string inputType, string expectedType)
    {
        var tenantId = Guid.NewGuid();
        var request = new CreateReportRequest(inputType, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);

        GeneratedReport? captured = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<GeneratedReport>(), It.IsAny<CancellationToken>()))
            .Callback<GeneratedReport, CancellationToken>((report, _) => captured = report)
            .Returns(Task.CompletedTask);

        var result = await _useCase.RequestReportAsync(tenantId, Guid.NewGuid(), "Admin", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(expectedType, captured.ReportType);
        Assert.Equal("PENDING", captured.Status);
        _jobQueue.Verify(q => q.EnqueueAsync(captured.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestReportAsync_WithWeeklyGrouping_IncludesGroupingInFileNameAndParameters()
    {
        var tenantId = Guid.NewGuid();
        var request = new CreateReportRequest(
            "SALES",
            new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 8, 23, 59, 59, TimeSpan.Zero),
            PeriodGrouping: "WEEKLY");

        GeneratedReport? captured = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<GeneratedReport>(), It.IsAny<CancellationToken>()))
            .Callback<GeneratedReport, CancellationToken>((report, _) => captured = report)
            .Returns(Task.CompletedTask);

        var result = await _useCase.RequestReportAsync(tenantId, Guid.NewGuid(), "Admin", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Contains("_weekly_", captured.FileName);
        Assert.Contains("\"PeriodGrouping\":\"WEEKLY\"", captured.ParametersJson);
    }

    [Fact]
    public async Task GetReportStatusAsync_WhenFound_ReturnsSuccess()
    {
        var tenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var existing = new GeneratedReport
        {
            Id = reportId,
            TenantId = tenantId,
            ReportType = "sales",
            Status = "COMPLETED",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repository.Setup(r => r.GetByIdAsync(tenantId, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _useCase.GetReportStatusAsync(tenantId, reportId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(reportId, result.Value!.Id);
        Assert.Equal("sales", result.Value.ReportType);
    }

    [Fact]
    public async Task GetReportStatusAsync_WhenNotFound_ReturnsNotFoundError()
    {
        var tenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        _repository.Setup(r => r.GetByIdAsync(tenantId, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeneratedReport?)null);

        var result = await _useCase.GetReportStatusAsync(tenantId, reportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("REPORT_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task DownloadReportAsync_WhenNotCompleted_ReturnsReportNotReadyError()
    {
        var tenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var report = new GeneratedReport
        {
            Id = reportId,
            TenantId = tenantId,
            ReportType = "sales",
            Status = "PROCESSING",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repository.Setup(r => r.GetByIdAsync(tenantId, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _useCase.DownloadReportAsync(tenantId, reportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("REPORT_NOT_READY", result.Error.Code);
    }

    [Fact]
    public async Task ProcessReportJobAsync_WhenGeneratorThrows_MarksReportAsFailed()
    {
        var reportId = Guid.NewGuid();
        var report = new GeneratedReport
        {
            Id = reportId,
            TenantId = Guid.NewGuid(),
            ReportType = "sales_and_expenses",
            Status = "PENDING",
            ParametersJson = "{}",
            FileName = "Report.xlsx",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repository.Setup(r => r.GetByIdForUpdateAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _generator.Setup(g => g.GenerateExcelAsync(It.IsAny<Guid>(), It.IsAny<GeneratedReport>(), It.IsAny<CreateReportRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Excel generation crash"));

        var result = await _useCase.ProcessReportJobAsync(reportId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("FAILED", report.Status);
        Assert.Equal("Excel generation crash", report.ErrorMessage);
        Assert.NotNull(report.CompletedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(CancellationToken.None), Times.AtLeastOnce);
    }
}
