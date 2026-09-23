using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SaviaUp.Backend.Domain.DTOs;

public record CashRegisterDto(
    Guid Id,
    string Name,
    string? Location,
    bool IsActive,
    bool HasOpenShift,
    Guid? ActiveShiftId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateCashRegisterRequest(
    string Name,
    string? Location,
    bool IsActive = true);

public record UpdateCashRegisterRequest(
    string Name,
    string? Location,
    bool IsActive);

public record SetCashRegisterStatusRequest(
    bool IsActive);

public record OpeningBalanceInputDto(
    [Required] string MethodName,
    [Range(0, 999999999999)] decimal Amount);

public record OpenCashRegisterShiftRequest(
    [Required] Guid CashRegisterId,
    IReadOnlyCollection<OpeningBalanceInputDto>? InitialBalances = null);

public record ClosingBalanceInputDto(
    [Required] string MethodName,
    [Range(0, 999999999999)] decimal ActualAmount);

public record CloseCashRegisterShiftRequest(
    IReadOnlyCollection<ClosingBalanceInputDto>? ClosingBalances = null);

public record PaymentMethodClosingSummaryDto(
    string MethodName,
    decimal InitialOpeningAmount,
    decimal SalesCollectedAmount,
    decimal TipsCollectedAmount,
    decimal ExpensesAmount,
    decimal TotalCollectedAmount,
    decimal ExpectedTotalAmount,
    decimal ActualAmount,
    decimal DifferenceAmount);

public record CashRegisterShiftSummaryDto(
    Guid ShiftId,
    Guid CashRegisterId,
    string CashRegisterName,
    string Status,
    string OpenedByUserName,
    DateTimeOffset OpenedAt,
    decimal TotalSalesAmount,
    decimal TotalTipsAmount,
    decimal TotalCollectedAmount,
    decimal TotalExpensesAmount,
    IReadOnlyCollection<PaymentMethodClosingSummaryDto> MethodSummaries)
{
    public decimal InitialOpeningAmount => MethodSummaries.Sum(method => method.InitialOpeningAmount);

    public decimal TotalInCashAmount => InitialOpeningAmount + TotalCollectedAmount - TotalExpensesAmount;
}

public record CashRegisterShiftDto(
    Guid Id,
    Guid CashRegisterId,
    string CashRegisterName,
    string Status,
    Guid OpenedByUserId,
    string OpenedByUserName,
    DateTimeOffset OpenedAt,
    Guid? ClosedByUserId,
    string? ClosedByUserName,
    DateTimeOffset? ClosedAt,
    decimal TotalSalesAmount,
    decimal TotalTipsAmount,
    decimal TotalCollectedAmount,
    decimal TotalExpensesAmount,
    string OpeningBalancesJson,
    string? ClosingSummaryJson)
{
    public decimal InitialOpeningAmount => SumOpeningBalances(OpeningBalancesJson);

    public decimal TotalInCashAmount => InitialOpeningAmount + TotalCollectedAmount - TotalExpensesAmount;

    private static decimal SumOpeningBalances(string openingBalancesJson)
    {
        if (string.IsNullOrWhiteSpace(openingBalancesJson)) return 0;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var balances = JsonSerializer.Deserialize<IReadOnlyCollection<OpeningBalanceInputDto>>(
                openingBalancesJson,
                options);

            return balances?.Sum(balance => balance.Amount) ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}

public record CashRegisterShiftQueryRequest(
    int Page = 1,
    int PageSize = 25,
    Guid? CashRegisterId = null,
    string? Status = null);
