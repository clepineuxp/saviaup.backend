namespace SaviaUp.Backend.Domain.Entities;

public sealed class GeneratedReport
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ReportType { get; set; } = string.Empty; // statistics, expenses, orders, sales_expenses, cash_registers
    public string Status { get; set; } = "PENDING"; // PENDING, PROCESSING, COMPLETED, FAILED
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
    public string ParametersJson { get; set; } = "{}";
    public string FileName { get; set; } = string.Empty;
    public string? FileReference { get; set; }
    public long? FileSize { get; set; }
    public string? ErrorMessage { get; set; }

    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

