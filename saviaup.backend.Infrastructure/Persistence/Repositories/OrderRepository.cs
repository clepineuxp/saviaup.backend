using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public async Task<PageData<OrderDto>> GetOrdersPageAsync(
        Guid tenantId,
        OrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .Where(o => o.TenantId == tenantId);

        if (request.TableId.HasValue)
        {
            query = query.Where(o => o.TableId == request.TableId.Value);
        }

        if (request.Statuses is not null && request.Statuses.Count > 0)
        {
            query = query.Where(o => request.Statuses.Contains(o.Status));
        }

        if (request.FromDate.HasValue)
        {
            var fromUtc = request.FromDate.Value.ToUniversalTime();
            query = query.Where(o => o.CreatedAt >= fromUtc);
        }

        if (request.ToDate.HasValue)
        {
            var toUtc = request.ToDate.Value.ToUniversalTime();
            query = query.Where(o => o.CreatedAt <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToString().Contains(s) ||
                (o.Table != null && o.Table.Name.ToLower().Contains(s)) ||
                o.CreatedByUserName.ToLower().Contains(s) ||
                (o.PaidByUserName != null && o.PaidByUserName.ToLower().Contains(s)) ||
                o.Items.Any(i => i.ProductName.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(o => new OrderDto(
            o.Id,
            o.TenantId,
            o.TableId,
            o.Table?.Name,
            o.OrderNumber,
            o.Status,
            o.SubtotalAmount,
            o.TaxAmount,
            o.TipAmount,
            o.TotalAmount,
            o.PaymentMethod,
            o.PaymentDetailsJson,
            o.Observations,
            o.CreatedByUserId,
            o.CreatedByUserName,
            o.LastModifiedByUserId,
            o.LastModifiedByUserName,
            o.PaidByUserId,
            o.PaidByUserName,
            o.PaidAt,
            o.CreatedAt,
            o.UpdatedAt,
            o.Items.OrderBy(i => i.CreatedAt).Select(i => new OrderItemDto(
                i.Id,
                i.OrderId,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.Subtotal,
                i.Status,
                i.Notes,
                i.IsCustomSale,
                i.CancellationReason,
                i.CancelledAt,
                i.CancelledByUserId,
                i.CancelledByUserName,
                i.CreatedByUserId,
                i.CreatedByUserName,
                i.LastModifiedByUserId,
                i.LastModifiedByUserName,
                i.CreatedAt,
                i.UpdatedAt)).ToList()
        )).ToList();

        return new PageData<OrderDto>(dtos, totalCount);
    }

    public async Task<PageData<OrderItemReportDto>> GetOrderItemsPageAsync(
        Guid tenantId,
        OrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.OrderItems
            .Include(i => i.Order)
                .ThenInclude(o => o!.Table)
            .Where(i => i.Order != null && i.Order.TenantId == tenantId);

        if (request.TableId.HasValue)
        {
            query = query.Where(i => i.Order!.TableId == request.TableId.Value);
        }

        if (request.Statuses is not null && request.Statuses.Count > 0)
        {
            query = query.Where(i => request.Statuses.Contains(i.Status));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(i =>
                i.ProductName.ToLower().Contains(s) ||
                (i.Notes != null && i.Notes.ToLower().Contains(s)) ||
                i.Order!.OrderNumber.ToString().Contains(s) ||
                (i.Order.Table != null && i.Order.Table.Name.ToLower().Contains(s)) ||
                i.CreatedByUserName.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(i => new OrderItemReportDto(
            i.Id,
            i.OrderId,
            i.Order!.OrderNumber,
            i.Order.Table?.Name,
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.Subtotal,
            i.Status,
            i.Notes,
            i.IsCustomSale,
            i.CancellationReason,
            i.CancelledAt,
            i.CancelledByUserId,
            i.CancelledByUserName,
            i.CreatedByUserId,
            i.CreatedByUserName,
            i.CreatedAt,
            i.UpdatedAt
        )).ToList();

        return new PageData<OrderItemReportDto>(dtos, totalCount);
    }

    public async Task<Order?> GetActiveByTableIdAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.TableId == tableId && o.Status == "PENDING", cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid tenantId, Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == orderId, cancellationToken);
    }

    public async Task<OrderItem?> GetItemByIdAsync(Guid tenantId, Guid itemId, CancellationToken cancellationToken)
    {
        return await dbContext.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Order != null && i.Order.TenantId == tenantId && i.Id == itemId, cancellationToken);
    }

    public async Task<int> GetNextOrderNumberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var max = await dbContext.Orders
            .Where(o => o.TenantId == tenantId)
            .MaxAsync(o => (int?)o.OrderNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task AddItemAsync(OrderItem item, CancellationToken cancellationToken)
    {
        await dbContext.OrderItems.AddAsync(item, cancellationToken);
    }

    public void RemoveItem(OrderItem item)
    {
        dbContext.OrderItems.Remove(item);
    }

    public async Task<IReadOnlyCollection<OrderReceiptDto>> GetReceiptsByOrderIdAsync(
        Guid tenantId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var receipts = await dbContext.OrderReceipts
            .Where(r => r.TenantId == tenantId && r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return receipts.Select(r => new OrderReceiptDto(
            r.Id,
            r.TenantId,
            r.OrderId,
            r.ReceiptNumber,
            r.ReceiptType,
            r.Title,
            r.SubtotalAmount,
            r.TaxAmount,
            r.TipAmount,
            r.TotalAmount,
            r.PaymentMethod,
            ParsePaymentDetails(r.PaymentDetailsJson),
            ParseReceiptItems(r.ItemsJson),
            r.IssuedByUserId,
            r.IssuedByUserName,
            r.CreatedAt)).ToList();
    }

    private static readonly System.Text.Json.JsonSerializerOptions ReceiptJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static IReadOnlyCollection<PaymentSplitDto>? ParsePaymentDetails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<PaymentSplitDto>>(json, ReceiptJsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyCollection<OrderReceiptItemDto> ParseReceiptItems(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<OrderReceiptItemDto>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<OrderReceiptItemDto>>(json, ReceiptJsonOptions) ?? new List<OrderReceiptItemDto>();
        }
        catch
        {
            return Array.Empty<OrderReceiptItemDto>();
        }
    }

    public async Task<int> GetNextReceiptNumberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var max = await dbContext.OrderReceipts
            .Where(r => r.TenantId == tenantId)
            .MaxAsync(r => (int?)r.ReceiptNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task AddReceiptAsync(OrderReceipt receipt, CancellationToken cancellationToken)
    {
        await dbContext.OrderReceipts.AddAsync(receipt, cancellationToken);
    }
}
