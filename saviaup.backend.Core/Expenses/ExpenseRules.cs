using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Core.Expenses;

public static class ExpenseRules
{
    public record struct PreparedExpenseValues(
        string Name,
        string NormalizedName,
        string? Description,
        decimal Amount,
        bool IsCashOut,
        string PaymentMethod,
        Guid? SupplierId,
        DateTimeOffset ExpenseDate);

    public static bool TryPrepare(
        string? name,
        string? description,
        decimal amount,
        bool isCashOut,
        string? paymentMethod,
        Guid? supplierId,
        DateTimeOffset? expenseDate,
        DateTimeOffset defaultNow,
        out PreparedExpenseValues values)
    {
        values = default;
        if (string.IsNullOrWhiteSpace(name) || amount <= 0 || string.IsNullOrWhiteSpace(paymentMethod))
            return false;

        var cleanName = SettingsDefaults.Normalize(name);
        if (cleanName.Length is 0 or > 160) return false;

        var cleanDesc = CleanOptional(description);
        if (cleanDesc?.Length > 1000) return false;

        var cleanPaymentMethod = SettingsDefaults.Normalize(paymentMethod);
        if (cleanPaymentMethod.Length is 0 or > 120) return false;

        var finalDate = expenseDate ?? defaultNow;

        values = new PreparedExpenseValues(
            cleanName,
            cleanName.ToUpperInvariant(),
            cleanDesc,
            amount,
            isCashOut,
            cleanPaymentMethod,
            supplierId == Guid.Empty ? null : supplierId,
            finalDate);
        return true;
    }

    public static ExpenseDto ToDto(Expense entity) => new(
        entity.Id,
        entity.ConsecutiveNumber,
        entity.Name,
        entity.Description,
        entity.Amount,
        entity.IsCashOut,
        entity.PaymentMethod,
        entity.Supplier is null ? null : new ExpenseSupplierDto(entity.Supplier.Id, entity.Supplier.Name),
        entity.ExpenseDate,
        entity.Status,
        entity.AnnulledReason,
        entity.AnnulledAt,
        entity.AnnulledByUserName,
        entity.CreatedByUserName,
        entity.LastModifiedByUserName,
        entity.CreatedAt,
        entity.UpdatedAt);

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : SettingsDefaults.Normalize(value);
}
