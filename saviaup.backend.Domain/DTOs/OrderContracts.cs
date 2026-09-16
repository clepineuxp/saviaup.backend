using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record OrderItemDto(
    Guid Id,
    Guid OrderId,
    Guid? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    string Status,
    string? Notes,
    bool IsCustomSale,
    string? CancellationReason,
    DateTimeOffset? CancelledAt,
    Guid? CancelledByUserId,
    string? CancelledByUserName,
    Guid CreatedByUserId,
    string CreatedByUserName,
    Guid? LastModifiedByUserId,
    string? LastModifiedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrderItemReportDto(
    Guid ItemId,
    Guid OrderId,
    int OrderNumber,
    string? TableName,
    Guid? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    string Status,
    string? Notes,
    bool IsCustomSale,
    string? CancellationReason,
    DateTimeOffset? CancelledAt,
    Guid? CancelledByUserId,
    string? CancelledByUserName,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrderDto(
    Guid Id,
    Guid TenantId,
    Guid? TableId,
    string? TableName,
    int OrderNumber,
    string Status,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TipAmount,
    decimal TotalAmount,
    string? PaymentMethod,
    string? PaymentDetailsJson,
    string? Observations,
    Guid CreatedByUserId,
    string CreatedByUserName,
    Guid? LastModifiedByUserId,
    string? LastModifiedByUserName,
    Guid? PaidByUserId,
    string? PaidByUserName,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<OrderItemDto> Items);

public sealed record OrderQueryRequest(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    IReadOnlyCollection<string>? Statuses = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    Guid? TableId = null);

public sealed record CreateOrderItemRequest(
    Guid? ProductId,
    [Required, MaxLength(160)] string ProductName,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal UnitPrice,
    [Range(1, 10000)] int Quantity,
    [MaxLength(500)] string? Notes,
    bool IsCustomSale);

public sealed record AddOrderItemsRequest(
    [Required, MinLength(1)] IReadOnlyCollection<CreateOrderItemRequest> Items,
    [MaxLength(500)] string? Observations = null);

public sealed record CancelOrderItemRequest(
    [Required, MaxLength(300)] string Reason);

public sealed record MoveTableOrderRequest(
    [Required] Guid TargetTableId);

public sealed record PaymentSplitDto(
    [Required, MaxLength(120)] string Method,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Amount);

public sealed record PartialItemPayDto(
    [Required] Guid ItemId,
    [Range(1, 10000)] int Quantity);

public sealed record CheckoutOrderRequest(
    [Required, MaxLength(120)] string PaymentMethod,
    IReadOnlyCollection<PaymentSplitDto>? Splits = null,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal TipAmount = 0,
    IReadOnlyCollection<PartialItemPayDto>? ItemsToPay = null);

public sealed record OrderReceiptItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderReceiptDto(
    Guid Id,
    Guid TenantId,
    Guid OrderId,
    int ReceiptNumber,
    string ReceiptType,
    string Title,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TipAmount,
    decimal TotalAmount,
    string? PaymentMethod,
    IReadOnlyCollection<PaymentSplitDto>? PaymentDetails,
    IReadOnlyCollection<OrderReceiptItemDto> Items,
    Guid IssuedByUserId,
    string IssuedByUserName,
    DateTimeOffset CreatedAt);
