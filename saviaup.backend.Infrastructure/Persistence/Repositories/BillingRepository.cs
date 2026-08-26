using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class BillingRepository(ApplicationDbContext dbContext) : IBillingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<PageData<BillingReceiptItemDto>> GetReceiptsPageAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var query = dbContext.OrderReceipts
            .Include(r => r.Order)
                .ThenInclude(o => o!.Table)
            .Where(r => r.TenantId == tenantId);

        if (request.FromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.ReceiptNumber.ToString().Contains(search) ||
                (r.Order != null && r.Order.OrderNumber.ToString().Contains(search)) ||
                (r.Order != null && r.Order.Table != null && r.Order.Table.Name.ToLower().Contains(search)) ||
                r.IssuedByUserName.ToLower().Contains(search) ||
                (r.Order != null && r.Order.PaidByUserName != null && r.Order.PaidByUserName.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var receipts = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = receipts.Select(r =>
        {
            var items = DeserializeItems(r.ItemsJson);
            return new BillingReceiptItemDto(
                r.Id,
                r.ReceiptNumber,
                r.ReceiptType,
                r.Title,
                r.OrderId,
                r.Order?.OrderNumber ?? 0,
                r.Order?.TableId,
                r.Order?.Table?.Name ?? "Sin Mesa",
                r.IssuedByUserId,
                r.IssuedByUserName,
                r.Order?.PaidByUserName ?? r.IssuedByUserName,
                r.PaymentMethod,
                r.SubtotalAmount,
                r.TaxAmount,
                r.TipAmount,
                r.TotalAmount,
                r.CreatedAt,
                items.Count);
        }).ToList();

        return new PageData<BillingReceiptItemDto>(dtos, totalCount);
    }

    public async Task<PageData<BillingOrderDto>> GetOrdersPageAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var query = dbContext.Orders
            .Include(o => o.Table)
            .Include(o => o.Receipts)
            .Where(o => o.TenantId == tenantId && o.Receipts.Any());

        if (request.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToString().Contains(search) ||
                (o.Table != null && o.Table.Name.ToLower().Contains(search)) ||
                o.CreatedByUserName.ToLower().Contains(search) ||
                (o.PaidByUserName != null && o.PaidByUserName.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = orders.Select(o =>
        {
            var receiptDtos = o.Receipts
                .OrderByDescending(r => r.CreatedAt)
                .Select(MapToReceiptDto)
                .ToList();

            return new BillingOrderDto(
                o.Id,
                o.OrderNumber,
                o.TableId,
                o.Table?.Name ?? "Sin Mesa",
                o.Status,
                o.SubtotalAmount,
                o.TaxAmount,
                o.TipAmount,
                o.TotalAmount,
                o.PaymentMethod,
                o.CreatedByUserName,
                o.PaidByUserName,
                o.PaidAt,
                o.CreatedAt,
                o.Receipts.Count,
                receiptDtos);
        }).ToList();

        return new PageData<BillingOrderDto>(dtos, totalCount);
    }

    public async Task<OrderReceiptDto?> GetReceiptByIdAsync(
        Guid tenantId,
        Guid receiptId,
        CancellationToken cancellationToken)
    {
        var receipt = await dbContext.OrderReceipts
            .Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == receiptId, cancellationToken);

        return receipt is null ? null : MapToReceiptDto(receipt);
    }

    private static OrderReceiptDto MapToReceiptDto(OrderReceipt r)
    {
        var items = DeserializeItems(r.ItemsJson);
        var splits = DeserializeSplits(r.PaymentDetailsJson);

        return new OrderReceiptDto(
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
            splits,
            items,
            r.IssuedByUserId,
            r.IssuedByUserName,
            r.CreatedAt);
    }

    private static IReadOnlyCollection<OrderReceiptItemDto> DeserializeItems(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<OrderReceiptItemDto>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyCollection<PaymentSplitDto>? DeserializeSplits(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<PaymentSplitDto>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
