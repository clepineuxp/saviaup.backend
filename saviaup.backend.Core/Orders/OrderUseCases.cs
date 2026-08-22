using System.Text.Json;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Core.Tables;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Orders;

public static class OrderRules
{
    public static OrderDto ToDto(Order order) => new(
        order.Id,
        order.TenantId,
        order.TableId,
        order.Table?.Name,
        order.OrderNumber,
        order.Status,
        order.SubtotalAmount,
        order.TaxAmount,
        order.TipAmount,
        order.TotalAmount,
        order.PaymentMethod,
        order.PaymentDetailsJson,
        order.Observations,
        order.CreatedByUserId,
        order.CreatedByUserName,
        order.LastModifiedByUserId,
        order.LastModifiedByUserName,
        order.PaidByUserId,
        order.PaidByUserName,
        order.PaidAt,
        order.CreatedAt,
        order.UpdatedAt,
        order.Items.OrderBy(item => item.CreatedAt).Select(ToDto).ToArray());

    public static OrderItemDto ToDto(OrderItem item) => new(
        item.Id,
        item.OrderId,
        item.ProductId,
        item.ProductName,
        item.UnitPrice,
        item.Quantity,
        item.Subtotal,
        item.Status,
        item.Notes,
        item.IsCustomSale,
        item.CancellationReason,
        item.CancelledAt,
        item.CancelledByUserId,
        item.CancelledByUserName,
        item.CreatedByUserId,
        item.CreatedByUserName,
        item.LastModifiedByUserId,
        item.LastModifiedByUserName,
        item.CreatedAt,
        item.UpdatedAt);

    public static void RecalculateTotals(Order order)
    {
        var activeItems = order.Items.Where(item => item.Status != "CANCELLED").ToList();
        order.SubtotalAmount = activeItems.Sum(item => item.Subtotal);
        order.TotalAmount = order.SubtotalAmount + order.TipAmount;
    }

    public static PagedResponse<OrderDto> ToPage(PageData<OrderDto> page, int pageNumber, int pageSize)
    {
        var totalPages = (int)Math.Ceiling(page.TotalCount / (double)pageSize);
        return new PagedResponse<OrderDto>(page.Items, pageNumber, pageSize, page.TotalCount, totalPages);
    }
}

public sealed class GetOrdersPageUseCase(IOrderRepository orderRepository) : IGetOrdersPageUseCase
{
    public async Task<Result<PagedResponse<OrderDto>>> ExecuteAsync(
        Guid tenantId,
        OrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var page = await orderRepository.GetOrdersPageAsync(tenantId, request, cancellationToken);
        return Result<PagedResponse<OrderDto>>.Success(OrderRules.ToPage(page, request.Page, request.PageSize));
    }
}

public sealed class GetOrderItemsPageUseCase(IOrderRepository orderRepository) : IGetOrderItemsPageUseCase
{
    public async Task<Result<PagedResponse<OrderItemReportDto>>> ExecuteAsync(
        Guid tenantId,
        OrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var page = await orderRepository.GetOrderItemsPageAsync(tenantId, request, cancellationToken);
        var totalPages = (int)Math.Ceiling(page.TotalCount / (double)request.PageSize);
        var response = new PagedResponse<OrderItemReportDto>(page.Items, request.Page, request.PageSize, page.TotalCount, totalPages);
        return Result<PagedResponse<OrderItemReportDto>>.Success(response);
    }
}

public sealed class GetActiveTableOrderUseCase(IOrderRepository orderRepository) : IGetActiveTableOrderUseCase
{
    public async Task<Result<OrderDto>> ExecuteAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetActiveByTableIdAsync(tenantId, tableId, cancellationToken);
        if (order is null) return Result<OrderDto>.Failure(Errors.Validation);
        return Result<OrderDto>.Success(OrderRules.ToDto(order));
    }
}

public sealed class AddTableOrderItemsUseCase(
    IOrderRepository orderRepository,
    IRestaurantTableRepository tableRepository,
    ITenantRepository tenantRepository,
    ICashRegisterShiftRepository shiftRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IAddTableOrderItemsUseCase
{
    public async Task<Result<OrderDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        Guid userId,
        string userName,
        AddOrderItemsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0 || request.Items.Any(item => string.IsNullOrWhiteSpace(item.ProductName) || item.UnitPrice <= 0 || item.Quantity <= 0))
            return Result<OrderDto>.Failure(Errors.Validation);

        var gate = await CheckGateAsync(tenantId, tenantRepository, shiftRepository, cancellationToken);
        if (!gate.IsSuccess) return Result<OrderDto>.Failure(gate.Error!);

        var table = await tableRepository.GetByIdAsync(tenantId, tableId, cancellationToken);
        if (table is null || table.Status == TableStatus.Disabled)
            return Result<OrderDto>.Failure(Errors.RestaurantTableNotFound);

        var now = clock.UtcNow;
        var order = await orderRepository.GetActiveByTableIdAsync(tenantId, tableId, cancellationToken);

        if (order is null)
        {
            var nextNumber = await orderRepository.GetNextOrderNumberAsync(tenantId, cancellationToken);
            var openShift = await shiftRepository.GetOpenShiftAsync(tenantId, null, cancellationToken);
            order = new Order
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TableId = tableId,
                CashRegisterShiftId = openShift?.Id,
                OrderNumber = nextNumber,
                Status = "PENDING",
                Observations = request.Observations?.Trim(),
                CreatedByUserId = userId,
                CreatedByUserName = userName,
                CreatedAt = now,
                UpdatedAt = now
            };
            await orderRepository.AddAsync(order, cancellationToken);

            table.Status = TableStatus.Occupied;
            table.ActiveOrderId = order.Id;
            table.OccupiedAt ??= now;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.Observations))
            {
                order.Observations = string.IsNullOrWhiteSpace(order.Observations)
                    ? request.Observations.Trim()
                    : $"{order.Observations}; {request.Observations.Trim()}";
            }
            order.LastModifiedByUserId = userId;
            order.LastModifiedByUserName = userName;
            order.UpdatedAt = now;
        }

        foreach (var reqItem in request.Items)
        {
            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = reqItem.ProductId,
                ProductName = reqItem.ProductName.Trim(),
                UnitPrice = reqItem.UnitPrice,
                Quantity = reqItem.Quantity,
                Subtotal = reqItem.UnitPrice * reqItem.Quantity,
                Status = "PENDING",
                Notes = reqItem.Notes?.Trim(),
                IsCustomSale = reqItem.IsCustomSale,
                CreatedByUserId = userId,
                CreatedByUserName = userName,
                CreatedAt = now,
                UpdatedAt = now
            };
            order.Items.Add(item);
            await orderRepository.AddItemAsync(item, cancellationToken);
        }

        OrderRules.RecalculateTotals(order);
        table.ActiveOrderTotal = order.TotalAmount;
        table.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var tableDto = TableRules.ToDto(table);
        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(tableDto), cancellationToken);
        await realtime.OrderUpdatedAsync(tenantId, new TableOrderUpdatedEvent(table.Id, order.Id, order.TotalAmount, now), cancellationToken);

        return Result<OrderDto>.Success(OrderRules.ToDto(order));
    }

    private static async Task<Result> CheckGateAsync(
        Guid tenantId,
        ITenantRepository tenantRepository,
        ICashRegisterShiftRepository shiftRepository,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(Errors.TenantNotFound);
        if (tenant.RequiresOpenCashRegister && !await shiftRepository.HasOpenShiftAsync(tenantId, cancellationToken))
            return Result.Failure(Errors.CashRegisterClosed);
        return Result.Success();
    }
}

public sealed class MoveTableOrderUseCase(
    IOrderRepository orderRepository,
    IRestaurantTableRepository tableRepository,
    ITenantRepository tenantRepository,
    ICashRegisterShiftRepository shiftRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IMoveTableOrderUseCase
{
    public async Task<Result<RestaurantTableDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        MoveTableOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TargetTableId == Guid.Empty || request.TargetTableId == tableId)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<RestaurantTableDto>.Failure(Errors.TenantNotFound);
        if (tenant.RequiresOpenCashRegister && !await shiftRepository.HasOpenShiftAsync(tenantId, cancellationToken))
            return Result<RestaurantTableDto>.Failure(Errors.CashRegisterClosed);

        var sourceTable = await tableRepository.GetByIdAsync(tenantId, tableId, cancellationToken);
        if (sourceTable is null || sourceTable.Status != TableStatus.Occupied || !sourceTable.ActiveOrderId.HasValue)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);

        var targetTable = await tableRepository.GetByIdAsync(tenantId, request.TargetTableId, cancellationToken);
        if (targetTable is null || targetTable.Status == TableStatus.Disabled || targetTable.Status == TableStatus.Occupied)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);

        var order = await orderRepository.GetActiveByTableIdAsync(tenantId, tableId, cancellationToken);
        if (order is null) return Result<RestaurantTableDto>.Failure(Errors.Validation);

        var now = clock.UtcNow;
        order.TableId = targetTable.Id;
        order.UpdatedAt = now;

        targetTable.Status = TableStatus.Occupied;
        targetTable.ActiveOrderId = order.Id;
        targetTable.ActiveOrderTotal = order.TotalAmount;
        targetTable.OccupiedAt = sourceTable.OccupiedAt ?? now;
        targetTable.UpdatedAt = now;

        sourceTable.Status = TableStatus.Available;
        sourceTable.ActiveOrderId = null;
        sourceTable.ActiveOrderTotal = 0;
        sourceTable.OccupiedAt = null;
        sourceTable.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var sourceDto = TableRules.ToDto(sourceTable);
        var targetDto = TableRules.ToDto(targetTable);

        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(sourceDto), cancellationToken);
        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(targetDto), cancellationToken);

        return Result<RestaurantTableDto>.Success(targetDto);
    }
}

public sealed class CancelOrderItemUseCase(
    IOrderRepository orderRepository,
    IRestaurantTableRepository tableRepository,
    ITenantRepository tenantRepository,
    ICashRegisterShiftRepository shiftRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICancelOrderItemUseCase
{
    public async Task<Result<OrderDto>> ExecuteAsync(
        Guid tenantId,
        Guid itemId,
        Guid userId,
        string userName,
        CancelOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result<OrderDto>.Failure(Errors.Validation);

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<OrderDto>.Failure(Errors.TenantNotFound);
        if (tenant.RequiresOpenCashRegister && !await shiftRepository.HasOpenShiftAsync(tenantId, cancellationToken))
            return Result<OrderDto>.Failure(Errors.CashRegisterClosed);

        var item = await orderRepository.GetItemByIdAsync(tenantId, itemId, cancellationToken);
        if (item is null || item.Status == "CANCELLED")
            return Result<OrderDto>.Failure(Errors.Validation);

        var order = await orderRepository.GetByIdAsync(tenantId, item.OrderId, cancellationToken);
        if (order is null || order.Status == "PAID" || order.Status == "CANCELLED")
            return Result<OrderDto>.Failure(Errors.Validation);

        var now = clock.UtcNow;
        item.Status = "CANCELLED";
        item.CancellationReason = request.Reason.Trim();
        item.CancelledAt = now;
        item.CancelledByUserId = userId;
        item.CancelledByUserName = userName;
        item.LastModifiedByUserId = userId;
        item.LastModifiedByUserName = userName;
        item.UpdatedAt = now;

        order.LastModifiedByUserId = userId;
        order.LastModifiedByUserName = userName;
        order.UpdatedAt = now;

        OrderRules.RecalculateTotals(order);

        RestaurantTable? table = null;
        if (order.TableId.HasValue)
        {
            table = await tableRepository.GetByIdAsync(tenantId, order.TableId.Value, cancellationToken);
        }

        var activeItemsCount = order.Items.Count(i => i.Status != "CANCELLED");
        if (activeItemsCount == 0)
        {
            order.Status = "CANCELLED";
            if (table is not null)
            {
                table.Status = TableStatus.Available;
                table.ActiveOrderId = null;
                table.ActiveOrderTotal = 0;
                table.OccupiedAt = null;
                table.UpdatedAt = now;
            }
        }
        else if (table is not null)
        {
            table.ActiveOrderTotal = order.TotalAmount;
            table.UpdatedAt = now;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (table is not null)
        {
            var tableDto = TableRules.ToDto(table);
            await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(tableDto), cancellationToken);
            await realtime.OrderUpdatedAsync(tenantId, new TableOrderUpdatedEvent(table.Id, order.Id, order.TotalAmount, now), cancellationToken);
        }

        return Result<OrderDto>.Success(OrderRules.ToDto(order));
    }
}

public sealed class PayAndCloseTableOrderUseCase(
    IOrderRepository orderRepository,
    IRestaurantTableRepository tableRepository,
    ITenantRepository tenantRepository,
    ICashRegisterShiftRepository shiftRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IPayAndCloseTableOrderUseCase
{
    public async Task<Result<OrderDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        Guid userId,
        string userName,
        CheckoutOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            return Result<OrderDto>.Failure(Errors.Validation);

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<OrderDto>.Failure(Errors.TenantNotFound);
        if (tenant.RequiresOpenCashRegister && !await shiftRepository.HasOpenShiftAsync(tenantId, cancellationToken))
            return Result<OrderDto>.Failure(Errors.CashRegisterClosed);

        var table = await tableRepository.GetByIdAsync(tenantId, tableId, cancellationToken);
        if (table is null || table.Status != TableStatus.Occupied || !table.ActiveOrderId.HasValue)
            return Result<OrderDto>.Failure(Errors.Validation);

        var order = await orderRepository.GetActiveByTableIdAsync(tenantId, tableId, cancellationToken);
        if (order is null || order.Status == "PAID" || order.Status == "CANCELLED")
            return Result<OrderDto>.Failure(Errors.Validation);

        decimal expectedSubtotal;
        if (request.ItemsToPay is not null && request.ItemsToPay.Count > 0)
        {
            expectedSubtotal = request.ItemsToPay.Sum(payReq =>
            {
                var item = order.Items.FirstOrDefault(i => i.Id == payReq.ItemId && i.Status == "PENDING");
                return item is not null ? item.UnitPrice * Math.Min(payReq.Quantity, item.Quantity) : 0m;
            });
        }
        else
        {
            expectedSubtotal = order.Items.Where(i => i.Status == "PENDING").Sum(i => i.Subtotal);
        }

        if (expectedSubtotal <= 0)
            return Result<OrderDto>.Failure(Errors.Validation);

        var expectedTotal = expectedSubtotal + Math.Max(0m, request.TipAmount);

        if (request.Splits is not null && request.Splits.Count > 0)
        {
            var splitsTotal = request.Splits.Sum(s => s.Amount);
            if (splitsTotal < expectedTotal - 0.01m)
            {
                return Result<OrderDto>.Failure(Errors.Validation);
            }
        }

        var now = clock.UtcNow;
        var checkoutTip = Math.Max(0m, request.TipAmount);

        // 1. Construir la lista de splits de ESTA transacción de pago
        var newTransactionSplits = new List<PaymentSplitDto>();
        if (request.Splits is not null && request.Splits.Count > 0)
        {
            newTransactionSplits.AddRange(request.Splits.Where(s => !string.IsNullOrWhiteSpace(s.Method) && s.Amount > 0));
        }
        else
        {
            newTransactionSplits.Add(new PaymentSplitDto(request.PaymentMethod.Trim(), expectedTotal));
        }

        // 2. Acumular los splits existentes (si la orden ya tenía pagos parciales previos)
        var allSplits = new List<PaymentSplitDto>();
        if (!string.IsNullOrWhiteSpace(order.PaymentDetailsJson))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<List<PaymentSplitDto>>(order.PaymentDetailsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (existing is not null) allSplits.AddRange(existing);
            }
            catch { }
        }
        allSplits.AddRange(newTransactionSplits);

        order.PaymentDetailsJson = JsonSerializer.Serialize(allSplits);

        var distinctMethods = allSplits
            .Select(s => s.Method.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctMethods.Count == 1)
        {
            order.PaymentMethod = distinctMethods[0];
        }
        else
        {
            order.PaymentMethod = "MIXTO";
        }

        order.TipAmount += checkoutTip;

        // 3. Procesar cobros parciales por ítem y cantidad
        var transactionPaidItems = new List<OrderReceiptItemDto>();

        if (request.ItemsToPay is not null && request.ItemsToPay.Count > 0)
        {
            var newPaidItems = new List<OrderItem>();
            foreach (var payReq in request.ItemsToPay)
            {
                var existingItem = order.Items.FirstOrDefault(i => i.Id == payReq.ItemId && i.Status == "PENDING");
                if (existingItem is null) continue;

                var qtyToPay = Math.Min(payReq.Quantity, existingItem.Quantity);
                if (qtyToPay >= existingItem.Quantity)
                {
                    existingItem.Status = "PAID";
                    existingItem.LastModifiedByUserId = userId;
                    existingItem.LastModifiedByUserName = userName;
                    existingItem.UpdatedAt = now;

                    transactionPaidItems.Add(new OrderReceiptItemDto(existingItem.ProductName, existingItem.Quantity, existingItem.UnitPrice, existingItem.Subtotal));
                }
                else
                {
                    existingItem.Quantity -= qtyToPay;
                    existingItem.Subtotal = existingItem.UnitPrice * existingItem.Quantity;
                    existingItem.LastModifiedByUserId = userId;
                    existingItem.LastModifiedByUserName = userName;
                    existingItem.UpdatedAt = now;

                    var paidSubtotal = existingItem.UnitPrice * qtyToPay;
                    var paidSplitItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = existingItem.ProductId,
                        ProductName = existingItem.ProductName,
                        UnitPrice = existingItem.UnitPrice,
                        Quantity = qtyToPay,
                        Subtotal = paidSubtotal,
                        Status = "PAID",
                        Notes = existingItem.Notes,
                        IsCustomSale = existingItem.IsCustomSale,
                        CreatedByUserId = userId,
                        CreatedByUserName = userName,
                        LastModifiedByUserId = userId,
                        LastModifiedByUserName = userName,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    newPaidItems.Add(paidSplitItem);
                    transactionPaidItems.Add(new OrderReceiptItemDto(paidSplitItem.ProductName, paidSplitItem.Quantity, paidSplitItem.UnitPrice, paidSplitItem.Subtotal));
                }
            }

            foreach (var newItem in newPaidItems)
            {
                order.Items.Add(newItem);
                await orderRepository.AddItemAsync(newItem, cancellationToken);
            }
        }
        else
        {
            foreach (var item in order.Items)
            {
                if (item.Status == "PENDING")
                {
                    item.Status = "PAID";
                    item.LastModifiedByUserId = userId;
                    item.LastModifiedByUserName = userName;
                    item.UpdatedAt = now;

                    transactionPaidItems.Add(new OrderReceiptItemDto(item.ProductName, item.Quantity, item.UnitPrice, item.Subtotal));
                }
            }
        }

        OrderRules.RecalculateTotals(order);

        var hasRemainingPending = order.Items.Any(i => i.Status == "PENDING");
        if (!hasRemainingPending)
        {
            order.Status = "PAID";
            order.PaidAt = now;
            order.PaidByUserId = userId;
            order.PaidByUserName = userName;
            order.LastModifiedByUserId = userId;
            order.LastModifiedByUserName = userName;
            order.UpdatedAt = now;

            table.Status = TableStatus.Available;
            table.ActiveOrderId = null;
            table.ActiveOrderTotal = 0;
            table.OccupiedAt = null;
            table.UpdatedAt = now;
        }
        else
        {
            order.LastModifiedByUserId = userId;
            order.LastModifiedByUserName = userName;
            order.UpdatedAt = now;

            table.ActiveOrderTotal = order.Items.Where(i => i.Status == "PENDING").Sum(i => i.Subtotal);
            table.UpdatedAt = now;
        }

        // Crear y guardar el comprobante de pago en BD (Serializar con camelCase)
        var receiptNumber = await orderRepository.GetNextReceiptNumberAsync(tenantId, cancellationToken);
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var receipt = new OrderReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = order.Id,
            ReceiptNumber = receiptNumber,
            ReceiptType = "PAYMENT",
            Title = !hasRemainingPending ? "COMPROBANTE DE PAGO TOTAL" : "COMPROBANTE DE PAGO PARCIAL",
            SubtotalAmount = expectedSubtotal,
            TaxAmount = 0m,
            TipAmount = checkoutTip,
            TotalAmount = expectedTotal,
            PaymentMethod = request.PaymentMethod,
            PaymentDetailsJson = JsonSerializer.Serialize(newTransactionSplits, jsonOptions),
            ItemsJson = JsonSerializer.Serialize(transactionPaidItems, jsonOptions),
            IssuedByUserId = userId,
            IssuedByUserName = userName,
            CreatedAt = now
        };

        await orderRepository.AddReceiptAsync(receipt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tableDto = TableRules.ToDto(table);
        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(tableDto), cancellationToken);
        await realtime.OrderUpdatedAsync(tenantId, new TableOrderUpdatedEvent(table.Id, order.Id, table.ActiveOrderTotal, now), cancellationToken);

        return Result<OrderDto>.Success(OrderRules.ToDto(order));
    }
}

public sealed class GenerateSummaryReceiptUseCase(
    IOrderRepository orderRepository,
    IDateTimeProvider clock) : IGenerateSummaryReceiptUseCase
{
    public async Task<Result<OrderReceiptDto>> ExecuteAsync(
        Guid tenantId,
        Guid orderId,
        Guid userId,
        string userName,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(tenantId, orderId, cancellationToken);
        if (order is null) return Result<OrderReceiptDto>.Failure(Errors.Validation);

        var pendingItems = order.Items.Where(i => i.Status == "PENDING").ToList();
        if (pendingItems.Count == 0 && order.Items.Count == 0)
            return Result<OrderReceiptDto>.Failure(Errors.Validation);

        var itemsToSummarize = pendingItems.Count > 0 ? pendingItems : order.Items.Where(i => i.Status != "CANCELLED").ToList();
        var subtotal = itemsToSummarize.Sum(i => i.Subtotal);
        var total = subtotal + order.TipAmount;

        var itemsDto = itemsToSummarize.Select(i => new OrderReceiptItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Subtotal)).ToList();

        var now = clock.UtcNow;
        var receiptNumber = await orderRepository.GetNextReceiptNumberAsync(tenantId, cancellationToken);

        var receipt = new OrderReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = order.Id,
            ReceiptNumber = receiptNumber,
            ReceiptType = "PRE_BILLING",
            Title = "RESUMEN DE CUENTA (PRE-FACTURA)",
            SubtotalAmount = subtotal,
            TaxAmount = 0m,
            TipAmount = order.TipAmount,
            TotalAmount = total,
            PaymentMethod = order.PaymentMethod,
            PaymentDetailsJson = order.PaymentDetailsJson,
            ItemsJson = JsonSerializer.Serialize(itemsDto),
            IssuedByUserId = userId,
            IssuedByUserName = userName,
            CreatedAt = now
        };

        IReadOnlyCollection<PaymentSplitDto>? paymentDetails = null;
        if (!string.IsNullOrWhiteSpace(order.PaymentDetailsJson))
        {
            try { paymentDetails = JsonSerializer.Deserialize<List<PaymentSplitDto>>(order.PaymentDetailsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { }
        }

        // NOTA: El resumen de cuenta (pre-factura) no se guarda en BD, solo calcula los valores para la tirilla
        return Result<OrderReceiptDto>.Success(new OrderReceiptDto(
            receipt.Id,
            receipt.TenantId,
            receipt.OrderId,
            receipt.ReceiptNumber,
            receipt.ReceiptType,
            receipt.Title,
            receipt.SubtotalAmount,
            receipt.TaxAmount,
            receipt.TipAmount,
            receipt.TotalAmount,
            receipt.PaymentMethod,
            paymentDetails,
            itemsDto,
            receipt.IssuedByUserId,
            receipt.IssuedByUserName,
            receipt.CreatedAt));
    }
}

public sealed class GetOrderReceiptsUseCase(IOrderRepository orderRepository) : IGetOrderReceiptsUseCase
{
    public async Task<Result<IReadOnlyCollection<OrderReceiptDto>>> ExecuteAsync(
        Guid tenantId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var receipts = await orderRepository.GetReceiptsByOrderIdAsync(tenantId, orderId, cancellationToken);
        return Result<IReadOnlyCollection<OrderReceiptDto>>.Success(receipts);
    }
}
