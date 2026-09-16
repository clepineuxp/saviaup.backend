namespace SaviaUp.Backend.Domain.Entities;

public sealed class CashRegisterShift
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CashRegisterId { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN, CLOSED
    public Guid OpenedByUserId { get; set; }
    public string OpenedByUserName { get; set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; set; }
    public string OpeningBalancesJson { get; set; } = "[]";

    public Guid? ClosedByUserId { get; set; }
    public string? ClosedByUserName { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosingSummaryJson { get; set; }

    public decimal? TotalSalesAmount { get; set; }
    public decimal? TotalTipsAmount { get; set; }
    public decimal? TotalCollectedAmount { get; set; }
    public decimal? TotalExpensesAmount { get; set; }

    public CashRegister CashRegister { get; set; } = null!;
}
