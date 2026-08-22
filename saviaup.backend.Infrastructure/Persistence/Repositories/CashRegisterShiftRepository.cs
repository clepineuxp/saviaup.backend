using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class CashRegisterShiftRepository(SaviaUpDbContext context) : ICashRegisterShiftRepository
{
    public Task<bool> HasOpenShiftAsync(Guid tenantId, CancellationToken cancellationToken)
        => context.CashRegisterShifts.AsNoTracking()
            .AnyAsync(shift => shift.TenantId == tenantId && shift.Status == "OPEN", cancellationToken);

    public Task<CashRegisterShift?> GetOpenShiftAsync(Guid tenantId, Guid? cashRegisterId, CancellationToken cancellationToken)
    {
        var query = context.CashRegisterShifts
            .Include(shift => shift.CashRegister)
            .Where(shift => shift.TenantId == tenantId && shift.Status == "OPEN");

        if (cashRegisterId.HasValue && cashRegisterId.Value != Guid.Empty)
        {
            query = query.Where(shift => shift.CashRegisterId == cashRegisterId.Value);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<CashRegisterShift?> GetByIdAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken)
        => context.CashRegisterShifts
            .Include(shift => shift.CashRegister)
            .FirstOrDefaultAsync(shift => shift.TenantId == tenantId && shift.Id == shiftId, cancellationToken);

    public async Task<PageData<CashRegisterShiftDto>> GetShiftsPageAsync(Guid tenantId, CashRegisterShiftQueryRequest request, CancellationToken cancellationToken)
    {
        var query = context.CashRegisterShifts.AsNoTracking()
            .Include(shift => shift.CashRegister)
            .Where(shift => shift.TenantId == tenantId);

        if (request.CashRegisterId.HasValue && request.CashRegisterId.Value != Guid.Empty)
        {
            query = query.Where(shift => shift.CashRegisterId == request.CashRegisterId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusUpper = request.Status.Trim().ToUpperInvariant();
            query = query.Where(shift => shift.Status == statusUpper);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : request.PageSize;

        var items = await query
            .OrderByDescending(shift => shift.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(shift => new CashRegisterShiftDto(
                shift.Id,
                shift.CashRegisterId,
                shift.CashRegister.Name,
                shift.Status,
                shift.OpenedByUserId,
                shift.OpenedByUserName,
                shift.OpenedAt,
                shift.ClosedByUserId,
                shift.ClosedByUserName,
                shift.ClosedAt,
                shift.TotalSalesAmount ?? 0,
                shift.TotalTipsAmount ?? 0,
                shift.TotalCollectedAmount ?? 0,
                shift.TotalExpensesAmount ?? 0,
                shift.OpeningBalancesJson,
                shift.ClosingSummaryJson))
            .ToArrayAsync(cancellationToken);

        return new PageData<CashRegisterShiftDto>(items, totalCount);
    }

    public async Task<bool> HasOccupiedTablesOrPendingOrdersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var hasOccupiedTables = await context.RestaurantTables.AsNoTracking()
            .AnyAsync(table => table.TenantId == tenantId && (table.Status == TableStatus.Occupied || table.ActiveOrderId != null), cancellationToken);

        if (hasOccupiedTables) return true;

        var hasPendingOrders = await context.Orders.AsNoTracking()
            .AnyAsync(order => order.TenantId == tenantId && order.Status == "PENDING", cancellationToken);

        return hasPendingOrders;
    }

    public async Task AddAsync(CashRegisterShift shift, CancellationToken cancellationToken)
        => await context.CashRegisterShifts.AddAsync(shift, cancellationToken);
}
