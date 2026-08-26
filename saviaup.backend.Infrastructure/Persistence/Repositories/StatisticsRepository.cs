using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class StatisticsRepository(ApplicationDbContext dbContext) : IStatisticsRepository
{
    private static readonly string[] MonthNamesEs =
        ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    public async Task<StatisticsDashboardDto> GetDashboardStatisticsAsync(
        Guid tenantId,
        string period,
        bool includeTips,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var utcNow = now.ToUniversalTime();
        DateTimeOffset startDate;
        DateTimeOffset endDate;

        if (period == "last_30_days")
        {
            startDate = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, TimeSpan.Zero).AddDays(-29);
            endDate = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 23, 59, 59, 999, TimeSpan.Zero);
        }
        else
        {
            // current_month
            startDate = new DateTimeOffset(utcNow.Year, utcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var daysInMonth = DateTime.DaysInMonth(utcNow.Year, utcNow.Month);
            endDate = new DateTimeOffset(utcNow.Year, utcNow.Month, daysInMonth, 23, 59, 59, 999, TimeSpan.Zero);
        }

        // Fetch completed/paid orders within period range
        var orders = await dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.TenantId == tenantId &&
                        o.Status == "PAID" &&
                        o.CreatedAt >= startDate &&
                        o.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        // Daily trend data points
        var trendPoints = new List<DailySalesPointDto>();
        if (period == "current_month")
        {
            var daysInMonth = DateTime.DaysInMonth(utcNow.Year, utcNow.Month);
            for (var d = 1; d <= daysInMonth; d++)
            {
                var dayDate = new DateTime(utcNow.Year, utcNow.Month, d);
                var dayOrders = orders.Where(o => o.CreatedAt.Date == dayDate).ToList();

                var sales = dayOrders.Sum(o => o.SubtotalAmount);
                var tips = dayOrders.Sum(o => o.TipAmount);

                trendPoints.Add(new DailySalesPointDto(
                    dayDate.ToString("yyyy-MM-dd"),
                    d.ToString("D2"),
                    includeTips ? (sales + tips) : sales,
                    tips,
                    dayOrders.Count
                ));
            }
        }
        else
        {
            for (var i = 0; i < 30; i++)
            {
                var dayDate = startDate.Date.AddDays(i);
                if (dayDate > utcNow.Date) break;

                var dayOrders = orders.Where(o => o.CreatedAt.Date == dayDate).ToList();
                var sales = dayOrders.Sum(o => o.SubtotalAmount);
                var tips = dayOrders.Sum(o => o.TipAmount);

                trendPoints.Add(new DailySalesPointDto(
                    dayDate.ToString("yyyy-MM-dd"),
                    dayDate.Day.ToString("D2"),
                    includeTips ? (sales + tips) : sales,
                    tips,
                    dayOrders.Count
                ));
            }
        }

        // Summary Calculations
        var totalSalesBase = orders.Sum(o => o.SubtotalAmount);
        var totalTips = orders.Sum(o => o.TipAmount);
        var grandTotalSales = includeTips ? (totalSalesBase + totalTips) : totalSalesBase;
        var totalOrdersCount = orders.Count;
        var averageTicket = totalOrdersCount > 0 ? Math.Round(grandTotalSales / totalOrdersCount, 2) : 0m;

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

        // 6-Month Comparison Chart
        var monthComparisonList = new List<MonthlyComparisonPointDto>();
        var firstOfCurrentMonthUtc = new DateTimeOffset(utcNow.Year, utcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var sixMonthsAgoUtc = firstOfCurrentMonthUtc.AddMonths(-5);

        var historicalOrders = await dbContext.Orders
            .Where(o => o.TenantId == tenantId &&
                        o.Status == "PAID" &&
                        o.CreatedAt >= sixMonthsAgoUtc)
            .ToListAsync(cancellationToken);

        for (var m = 0; m < 6; m++)
        {
            var monthDate = sixMonthsAgoUtc.AddMonths(m);
            var mSales = historicalOrders
                .Where(o => o.CreatedAt.Year == monthDate.Year && o.CreatedAt.Month == monthDate.Month)
                .Sum(o => includeTips ? (o.SubtotalAmount + o.TipAmount) : o.SubtotalAmount);

            monthComparisonList.Add(new MonthlyComparisonPointDto(
                monthDate.Year,
                monthDate.Month,
                MonthNamesEs[monthDate.Month - 1],
                mSales
            ));
        }

        var summary = new SummaryStatisticsDto(
            grandTotalSales,
            totalOrdersCount,
            averageTicket,
            totalTips,
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
