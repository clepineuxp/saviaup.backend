using System.Text.Json;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Reports;

public sealed class ReportsUseCase(
    IGeneratedReportRepository reportRepository,
    IReportJobQueue reportJobQueue,
    IReportGenerator reportGenerator,
    IFileStorage fileStorage,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOrganizationTimeZone organizationTimeZone,
    ITimeZoneService timeZones) : IReportsUseCase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<GeneratedReportDto>> RequestReportAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<GeneratedReportDto>.Failure(Errors.TenantAccessDenied);
        }

        var normalizedType = NormalizeReportType(request.ReportType);
        if (normalizedType is null)
        {
            return Result<GeneratedReportDto>.Failure(Errors.Validation);
        }

        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        if (request.FromLocalDate.HasValue || request.ToLocalDate.HasValue)
        {
            if (request.FromLocalDate is not { } fromDay || request.ToLocalDate is not { } toDay ||
                fromDay > toDay || toDay == DateOnly.MaxValue)
                return Result<GeneratedReportDto>.Failure(Errors.Validation);

            request = request with
            {
                FromDate = timeZones.StartOfDayUtc(fromDay, zone),
                ToDate = timeZones.StartOfDayUtc(toDay.AddDays(1), zone).AddTicks(-1)
            };
        }

        if (request.FromDate == default || request.ToDate == default || request.FromDate > request.ToDate)
        {
            return Result<GeneratedReportDto>.Failure(Errors.Validation);
        }

        var normalizedPeriodGrouping = NormalizePeriodGrouping(request.PeriodGrouping);
        var normalizedRequest = request with
        {
            ReportType = normalizedType,
            PeriodGrouping = normalizedPeriodGrouping
        };

        var fromStr = timeZones.ConvertFromUtc(request.FromDate, zone).ToString("yyyyMMdd");
        var toStr = timeZones.ConvertFromUtc(request.ToDate, zone).ToString("yyyyMMdd");
        var groupSuffix = normalizedPeriodGrouping != "NONE" ? $"_{normalizedPeriodGrouping.ToLowerInvariant()}" : string.Empty;
        var fileName = $"Reporte_{normalizedType}{groupSuffix}_{fromStr}_{toStr}.xlsx";

        var parametersJson = JsonSerializer.Serialize(normalizedRequest, JsonOptions);

        var report = new GeneratedReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportType = normalizedType,
            Status = "PENDING",
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            ParametersJson = parametersJson,
            FileName = fileName,
            CreatedByUserId = userId,
            CreatedByUserName = string.IsNullOrWhiteSpace(userName) ? "Usuario" : userName.Trim(),
            CreatedAt = dateTimeProvider.UtcNow
        };

        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await reportJobQueue.EnqueueAsync(report.Id, cancellationToken);

        return Result<GeneratedReportDto>.Success(MapToDto(report));
    }

    private static string? NormalizeReportType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;
        var t = type.Trim().ToLowerInvariant();
        return t switch
        {
            "statistics" => "statistics",
            "expenses" => "expenses",
            "orders" or "sales" => "sales",
            "sales_expenses" or "sales_and_expenses" => "sales_and_expenses",
            "cash_registers" or "cashregisters" or "cajas" => "cash_registers",
            _ => null
        };
    }

    private static string NormalizePeriodGrouping(string? periodGrouping)
    {
        if (string.IsNullOrWhiteSpace(periodGrouping)) return "NONE";
        var p = periodGrouping.Trim().ToUpperInvariant();
        return p switch
        {
            "WEEKLY" or "WEEKS" or "SEMANAS" or "SEMANAL" => "WEEKLY",
            "MONTHLY" or "MONTHS" or "MESES" or "MENSUAL" => "MONTHLY",
            _ => "NONE"
        };
    }

    public async Task<Result<IReadOnlyCollection<GeneratedReportDto>>> ListReportsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<IReadOnlyCollection<GeneratedReportDto>>.Failure(Errors.TenantAccessDenied);
        }

        var list = await reportRepository.GetRecentAsync(tenantId, 50, cancellationToken);
        IReadOnlyCollection<GeneratedReportDto> dtos = list.Select(MapToDto).ToArray();
        return Result<IReadOnlyCollection<GeneratedReportDto>>.Success(dtos);
    }

    public async Task<Result<GeneratedReportDto>> GetReportStatusAsync(
        Guid tenantId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<GeneratedReportDto>.Failure(Errors.TenantAccessDenied);
        }

        var report = await reportRepository.GetByIdAsync(tenantId, reportId, cancellationToken);
        if (report is null)
        {
            return Result<GeneratedReportDto>.Failure(Errors.ReportNotFound);
        }

        return Result<GeneratedReportDto>.Success(MapToDto(report));
    }

    public async Task<Result<ReportDownloadDto>> DownloadReportAsync(
        Guid tenantId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<ReportDownloadDto>.Failure(Errors.TenantAccessDenied);
        }

        var report = await reportRepository.GetByIdAsync(tenantId, reportId, cancellationToken);
        if (report is null)
        {
            return Result<ReportDownloadDto>.Failure(Errors.ReportNotFound);
        }

        if (!string.Equals(report.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ReportDownloadDto>.Failure(Errors.ReportNotReady);
        }

        var fileReference = report.FileReference ?? report.FileName;
        var storedContent = await fileStorage.GetReportAsync(tenantId, fileReference, cancellationToken);
        if (storedContent is null)
        {
            return Result<ReportDownloadDto>.Failure(Errors.ReportNotFound);
        }

        return Result<ReportDownloadDto>.Success(new ReportDownloadDto(
            report.FileName,
            storedContent.ContentType,
            storedContent.Content));
    }

    public async Task<Result> ProcessReportJobAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdForUpdateAsync(reportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure(Errors.ReportNotFound);
        }

        report.Status = "PROCESSING";
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var parameters = JsonSerializer.Deserialize<CreateReportRequest>(report.ParametersJson, JsonOptions)
                ?? new CreateReportRequest(report.ReportType, report.FromDate, report.ToDate);

            var excelBytes = await reportGenerator.GenerateExcelAsync(
                report.TenantId,
                report,
                parameters,
                cancellationToken);

            var stored = await fileStorage.SaveReportAsync(
                report.TenantId,
                report.FileName,
                excelBytes,
                cancellationToken);

            report.Status = "COMPLETED";
            report.FileReference = stored.Reference;
            report.FileSize = excelBytes.Length;
            report.CompletedAt = dateTimeProvider.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            try
            {
                report.Status = "FAILED";
                report.ErrorMessage = ex.Message;
                report.CompletedAt = dateTimeProvider.UtcNow;
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
            catch
            {
                // Ignore failure to persist status during shutdown
            }

            return Result.Failure(Errors.ReportGenerationFailed);
        }
    }

    private static GeneratedReportDto MapToDto(GeneratedReport entity)
        => new(
            entity.Id,
            entity.ReportType,
            entity.Status,
            entity.FromDate,
            entity.ToDate,
            entity.FileName,
            entity.FileSize,
            entity.ErrorMessage,
            entity.ParametersJson,
            entity.CreatedByUserName,
            entity.CreatedAt,
            entity.CompletedAt);
}
