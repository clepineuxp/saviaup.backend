namespace SaviaUp.Backend.Domain.DTOs;

public sealed record CreateReportRequest(
    string ReportType,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string? GroupBy = null, // "COMMANDAS" | "PRODUCTS"
    string? CashFilter = null, // "ALL" | "ONLY_CASH_OUT" | "NO_CASH_OUT"
    bool? IncludeProducts = null,
    bool? IncludeTables = null,
    bool? IncludeWaiters = null,
    bool? IncludeCategories = null,
    string? PeriodGrouping = null); // "NONE" | "WEEKLY" | "MONTHLY"

public sealed record GeneratedReportDto(
    Guid Id,
    string ReportType,
    string Status,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string FileName,
    long? FileSize,
    string? ErrorMessage,
    string ParametersJson,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record ReportDownloadDto(
    string FileName,
    string ContentType,
    byte[] Content);

