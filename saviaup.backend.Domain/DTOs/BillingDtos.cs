namespace SaviaUp.Backend.Domain.DTOs;

public sealed record BillingReceiptQueryRequest(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);

public sealed record BillingReceiptItemDto(
    Guid ReceiptId,
    int ReceiptNumber,
    string ReceiptType,
    string Title,
    Guid OrderId,
    int OrderNumber,
    Guid? TableId,
    string? TableName,
    Guid IssuedByUserId,
    string IssuedByUserName,
    string? PaidByUserName,
    string? PaymentMethod,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TipAmount,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    int ItemsCount);

public sealed record BillingOrderDto(
    Guid OrderId,
    int OrderNumber,
    Guid? TableId,
    string? TableName,
    string Status,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TipAmount,
    decimal TotalAmount,
    string? PaymentMethod,
    string CreatedByUserName,
    string? PaidByUserName,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    int ReceiptsCount,
    IReadOnlyCollection<OrderReceiptDto> Receipts);
