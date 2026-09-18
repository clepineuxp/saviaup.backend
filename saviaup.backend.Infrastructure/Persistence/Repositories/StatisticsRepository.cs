using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class StatisticsRepository(ApplicationDbContext dbContext, IOrganizationTimeZone organizationTimeZone, ITimeZoneService timeZones) : IStatisticsRepository
{
    private static readonly string[] MonthNamesEs =
        ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    public async Task<StatisticsDashboardDto> GetDashboardStatisticsAsync(
        Guid tenantId,
        string period,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool includeTips,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var today = DateOnly.FromDateTime(timeZones.ConvertFromUtc(now, zone).DateTime);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        var startDay = period == "custom_range" ? fromDate!.Value : period == "last_30_days" ? today.AddDays(-29) : firstOfMonth;
        var endDay = period == "custom_range" ? toDate!.Value.AddDays(1) : period == "last_30_days" ? today.AddDays(1) : firstOfMonth.AddMonths(1);
        var startDate = timeZones.StartOfDayUtc(startDay, zone);
        var endDate = timeZones.StartOfDayUtc(endDay, zone);
        DateOnly LocalDay(DateTimeOffset instant) => DateOnly.FromDateTime(timeZones.ConvertFromUtc(instant, zone).DateTime);

        // Fetch completed/paid orders within period range
        var orders = await dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.TenantId == tenantId &&
                        o.Status == "PAID" &&
                        o.PaidAt >= startDate &&
                        o.PaidAt < endDate)
            .ToListAsync(cancellationToken);

        // Fetch active expenses within period range
        var expenses = await dbContext.Expenses
            .Where(e => e.TenantId == tenantId &&
                        e.Status == "ACTIVE" &&
                        (e.BusinessDate.HasValue ? e.BusinessDate >= startDay && e.BusinessDate < endDay : e.ExpenseDate >= startDate && e.ExpenseDate < endDate))
            .ToListAsync(cancellationToken);

        // Group UTC instants by the organization's civil calendar, including DST.
        var trendPoints = new List<DailySalesPointDto>();
        var ordersByDay = orders.Where(o => o.PaidAt.HasValue).GroupBy(o => LocalDay(o.PaidAt!.Value)).ToDictionary(g => g.Key, g => g.ToList());
        for (var day = startDay; day < endDay; day = day.AddDays(1))
        {
            var dayOrders = ordersByDay.GetValueOrDefault(day) ?? [];
            var sales = dayOrders.Sum(o => o.SubtotalAmount);
            var tips = dayOrders.Sum(o => o.TipAmount);
            trendPoints.Add(new DailySalesPointDto(day.ToString("yyyy-MM-dd"), day.Day.ToString("D2"), includeTips ? sales + tips : sales, tips, dayOrders.Count));
        }

        // Summary Calculations
        var totalSalesBase = orders.Sum(o => o.SubtotalAmount);
        var totalTips = orders.Sum(o => o.TipAmount);
        var grandTotalSales = includeTips ? (totalSalesBase + totalTips) : totalSalesBase;
        var totalOrdersCount = orders.Count;
        var averageTicket = totalOrdersCount > 0 ? Math.Round(grandTotalSales / totalOrdersCount, 2) : 0m;
        var totalExpenses = expenses.Sum(e => e.Amount);

        // Sales By User
        var userGrouped = orders
            .GroupBy(o => new { UserId = o.CreatedByUserId, Name = o.CreatedByUserName })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Name,
                Sales = g.Sum(o => includeTips ? (o.SubtotalAmount + o.TipAmount) : o.SubtotalAmount),
                OrdersCount = g.Count()
            })
            .OrderByDescending(u => u.Sales)
            .ToList();

        TopSellerDto? topSeller = null;
        if (userGrouped.Count > 0)
        {
            var top = userGrouped[0];
            topSeller = new TopSellerDto(top.UserId, top.Name, top.Sales, top.OrdersCount);
        }

        var salesByUser = userGrouped.Select(u => new UserSalesSummaryDto(
            u.UserId,
            u.Name,
            u.Sales,
            u.OrdersCount,
            IsTopSeller: topSeller != null && u.UserId == topSeller.UserId
        )).ToList();

        // Product Breakdown (Donut Charts)
        var allOrderItems = orders.SelectMany(o => o.Items).Where(i => i.Status != "CANCELLED").ToList();
        var productGrouped = allOrderItems
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                Name = g.Key.ProductName,
                Quantity = g.Sum(i => i.Quantity),
                Amount = g.Sum(i => i.Subtotal)
            })
            .ToList();

        var totalQtySum = productGrouped.Sum(p => p.Quantity);
        var totalAmountSum = productGrouped.Sum(p => p.Amount);

        var topProductsQty = productGrouped
            .OrderByDescending(p => p.Quantity)
            .Take(5)
            .Select(p => new ProductQuantityDto(
                p.ProductId,
                p.Name,
                p.Quantity,
                totalQtySum > 0 ? Math.Round((decimal)p.Quantity * 100m / totalQtySum, 1) : 0m
            ))
            .ToList();

        if (productGrouped.Count > 5)
        {
            var otherQty = productGrouped.OrderByDescending(p => p.Quantity).Skip(5).Sum(p => p.Quantity);
            topProductsQty.Add(new ProductQuantityDto(
                null,
                "Otros",
                otherQty,
                totalQtySum > 0 ? Math.Round((decimal)otherQty * 100m / totalQtySum, 1) : 0m
            ));
        }

        var topProductsVal = productGrouped
            .OrderByDescending(p => p.Amount)
            .Take(5)
            .Select(p => new ProductValueDto(
                p.ProductId,
                p.Name,
                p.Amount,
                totalAmountSum > 0 ? Math.Round(p.Amount * 100m / totalAmountSum, 1) : 0m
            ))
            .ToList();

        if (productGrouped.Count > 5)
        {
            var otherVal = productGrouped.OrderByDescending(p => p.Amount).Skip(5).Sum(p => p.Amount);
            topProductsVal.Add(new ProductValueDto(
                null,
                "Otros",
                otherVal,
                totalAmountSum > 0 ? Math.Round(otherVal * 100m / totalAmountSum, 1) : 0m
            ));
        }

        // 6-Month Comparison Chart (Sales & Expenses)
        var monthComparisonList = new List<MonthlyComparisonPointDto>();
        var comparisonStart = period == "custom_range"
            ? new DateOnly(startDay.Year, startDay.Month, 1)
            : firstOfMonth.AddMonths(-5);
        var comparisonEnd = period == "custom_range"
            ? new DateOnly(endDay.AddDays(-1).Year, endDay.AddDays(-1).Month, 1).AddMonths(1)
            : firstOfMonth.AddMonths(1);
        var comparisonStartUtc = timeZones.StartOfDayUtc(comparisonStart, zone);
        var comparisonEndUtc = timeZones.StartOfDayUtc(comparisonEnd, zone);

        var historicalOrders = await dbContext.Orders
            .Where(o => o.TenantId == tenantId &&
                        o.Status == "PAID" &&
                        o.PaidAt >= comparisonStartUtc && o.PaidAt < comparisonEndUtc)
            .ToListAsync(cancellationToken);

        var historicalExpenses = await dbContext.Expenses
            .Where(e => e.TenantId == tenantId &&
                        e.Status == "ACTIVE" &&
                        (e.BusinessDate.HasValue ? e.BusinessDate >= comparisonStart && e.BusinessDate < comparisonEnd : e.ExpenseDate >= comparisonStartUtc && e.ExpenseDate < comparisonEndUtc))
            .ToListAsync(cancellationToken);

        for (var monthDate = comparisonStart; monthDate < comparisonEnd; monthDate = monthDate.AddMonths(1))
        {
            var mSales = historicalOrders
                .Where(o => o.PaidAt.HasValue && LocalDay(o.PaidAt.Value).Year == monthDate.Year && LocalDay(o.PaidAt.Value).Month == monthDate.Month)
                .Sum(o => includeTips ? (o.SubtotalAmount + o.TipAmount) : o.SubtotalAmount);

            var mExpenses = historicalExpenses
                .Where(e => (e.BusinessDate ?? LocalDay(e.ExpenseDate)).Year == monthDate.Year && (e.BusinessDate ?? LocalDay(e.ExpenseDate)).Month == monthDate.Month)
                .Sum(e => e.Amount);

            monthComparisonList.Add(new MonthlyComparisonPointDto(
                monthDate.Year,
                monthDate.Month,
                MonthNamesEs[monthDate.Month - 1],
                mSales,
                mExpenses
            ));
        }

        var summary = new SummaryStatisticsDto(
            grandTotalSales,
            totalOrdersCount,
            averageTicket,
            totalTips,
            totalExpenses,
            topSeller
        );

        return new StatisticsDashboardDto(
            period,
            includeTips,
            summary,
            trendPoints,
            topProductsQty,
            topProductsVal,
            monthComparisonList,
            salesByUser
        );
    }
}
