using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record ExpenseSupplierDto(
    Guid Id,
    string Name);

public sealed record ExpenseDto(
    Guid Id,
    long ConsecutiveNumber,
    string Name,
    string? Description,
    decimal Amount,
    bool IsCashOut,
    string PaymentMethod,
    ExpenseSupplierDto? Supplier,
    DateTimeOffset ExpenseDate,
    string Status,
    string? AnnulledReason,
    DateTimeOffset? AnnulledAt,
    string? AnnulledByUserName,
    string? CreatedByUserName,
    string? LastModifiedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateExpenseRequest(
    [Required, MaxLength(160)] string Name,
    [MaxLength(1000)] string? Description,
    [Range(0.01, 999999999)] decimal Amount,
    bool IsCashOut,
    [Required, MaxLength(120)] string PaymentMethod,
    Guid? SupplierId,
    DateTimeOffset? ExpenseDate);

public sealed record UpdateExpenseRequest(
    [Required, MaxLength(160)] string Name,
    [MaxLength(1000)] string? Description,
    [Range(0.01, 999999999)] decimal Amount,
    bool IsCashOut,
    [Required, MaxLength(120)] string PaymentMethod,
    Guid? SupplierId,
    DateTimeOffset? ExpenseDate);

public sealed record AnnulExpenseRequest(
    [MaxLength(500)] string? Reason);

public sealed record ExpensePageDto(
    IReadOnlyCollection<ExpenseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
