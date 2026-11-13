using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Reports;

public sealed class ClosedXmlReportGenerator(ApplicationDbContext context, IOrganizationTimeZone organizationTimeZone, ITimeZoneService timeZones, IDateTimeProvider clock) : IReportGenerator
{
    private static readonly XLColor PrimaryHeaderBg = XLColor.FromHtml("#163300");
    private static readonly XLColor SecondaryHeaderBg = XLColor.FromHtml("#2D4A1D");
    private static readonly XLColor AccentHeaderBg = XLColor.FromHtml("#3B6E22");
    private static readonly XLColor SubtleBg = XLColor.FromHtml("#F4F7F2");
    private static readonly XLColor BorderColor = XLColor.FromHtml("#D1D5DB");

    public async Task<byte[]> GenerateExcelAsync(
        Guid tenantId,
        GeneratedReport report,
        CreateReportRequest parameters,
        CancellationToken cancellationToken)
    {
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        using var workbook = new XLWorkbook();

        var normalizedType = (report.ReportType ?? string.Empty).Trim().ToLowerInvariant();
        switch (normalizedType)
        {
            case "statistics":
                await BuildStatisticsReportAsync(workbook, tenantId, parameters, zone, cancellationToken);
                break;
            case "expenses":
                await BuildExpensesReportAsync(workbook, tenantId, parameters, zone, cancellationToken);
                break;
            case "orders":
            case "sales":
                await BuildOrdersReportAsync(workbook, tenantId, parameters, zone, cancellationToken);
                break;
            case "sales_expenses":
            case "sales_and_expenses":
                await BuildSalesAndExpensesReportAsync(workbook, tenantId, parameters, zone, cancellationToken);
                break;
            case "cash_registers":
            case "cashregisters":
            case "cajas":
                await BuildCashRegistersReportAsync(workbook, tenantId, parameters, zone, cancellationToken);
                break;
            default:
                await BuildOrdersReportAsync(workbook, tenantId, parameters, zone, cancellationToken);
                break;
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    private async Task BuildStatisticsReportAsync(
        XLWorkbook wb,
        Guid tenantId,
        CreateReportRequest req,
        string zone,
        CancellationToken ct)
    {
        var orders = await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.Status == "PAID" && o.PaidAt >= req.FromDate && o.PaidAt <= req.ToDate)
            .Include(o => o.Table)!.ThenInclude(t => t.DiningArea)
            .Include(o => o.Items)!.ThenInclude(i => i.Product)!.ThenInclude(p => p.Category)
            .ToListAsync(ct);

        var wsSummary = wb.Worksheets.Add("Resumen General");
        SetupTitle(wsSummary, "REPORTE DE ESTADÍSTICAS", req.FromDate, req.ToDate, zone);
        wsSummary.Cell(3, 1).Value = req.IncludeTips == true ? "Ventas con propinas" : "Ventas sin propinas";

        var totalSales = orders.Sum(o => (o.SubtotalAmount + (req.IncludeTips == true ? o.TipAmount : 0m)));
        var totalTips = orders.Sum(o => o.TipAmount);
        var totalOrders = orders.Count;
        var avgTicket = totalOrders > 0 ? Math.Round(totalSales / totalOrders, 2) : 0m;

        AddKpiCard(wsSummary, 4, 2, "Total Ventas Facturadas", totalSales, true);
        AddKpiCard(wsSummary, 4, 4, "Total Comandas / Pedidos", totalOrders, false);
        AddKpiCard(wsSummary, 4, 6, "Ticket Promedio", avgTicket, true);
        AddKpiCard(wsSummary, 4, 8, "Total Propinas", totalTips, true);

        var periods = GeneratePeriods(req.FromDate, req.ToDate, req.PeriodGrouping, zone);
        if (periods.Count > 0)
        {
            var pRow = 7;
            SetHeaders(wsSummary, pRow, "Período Agrupado", "Comandas Pagadas", "Ventas Totales ($)", "Ticket Promedio ($)", "Total Propinas ($)", "% del Total");
            foreach (var p in periods)
            {
                pRow++;
                var pOrders = orders.Where(o => o.PaidAt >= p.Start && o.PaidAt <= p.End).ToList();
                var pSales = pOrders.Sum(o => (o.SubtotalAmount + (req.IncludeTips == true ? o.TipAmount : 0m)));
                var pTips = pOrders.Sum(o => o.TipAmount);
                var pAvg = pOrders.Count > 0 ? Math.Round(pSales / pOrders.Count, 2) : 0m;

                wsSummary.Cell(pRow, 1).Value = p.Label;
                wsSummary.Cell(pRow, 2).Value = pOrders.Count;
                wsSummary.Cell(pRow, 3).Value = pSales;
                wsSummary.Cell(pRow, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsSummary.Cell(pRow, 4).Value = pAvg;
                wsSummary.Cell(pRow, 4).Style.NumberFormat.Format = "$#,##0.00";
                wsSummary.Cell(pRow, 5).Value = pTips;
                wsSummary.Cell(pRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                wsSummary.Cell(pRow, 6).Value = totalSales > 0 ? (pSales / totalSales) : 0m;
                wsSummary.Cell(pRow, 6).Style.NumberFormat.Format = "0.0%";
            }
            pRow++;
            wsSummary.Cell(pRow, 1).Value = "TOTAL CONSOLIDADO:";
            wsSummary.Cell(pRow, 1).Style.Font.Bold = true;
            wsSummary.Cell(pRow, 2).Value = totalOrders;
            wsSummary.Cell(pRow, 2).Style.Font.Bold = true;
            wsSummary.Cell(pRow, 3).Value = totalSales;
            wsSummary.Cell(pRow, 3).Style.NumberFormat.Format = "$#,##0.00";
            wsSummary.Cell(pRow, 3).Style.Font.Bold = true;
            wsSummary.Cell(pRow, 5).Value = totalTips;
            wsSummary.Cell(pRow, 5).Style.NumberFormat.Format = "$#,##0.00";
            wsSummary.Cell(pRow, 5).Style.Font.Bold = true;
            FormatDataRows(wsSummary, 8, pRow - 1, 6);
        }

        wsSummary.Columns().AdjustToContents();

        var includeProducts = req.IncludeProducts ?? true;
        var includeTables = req.IncludeTables ?? true;
        var includeWaiters = req.IncludeWaiters ?? true;
        var includeCategories = req.IncludeCategories ?? true;

        if (includeProducts)
        {
            var wsProducts = wb.Worksheets.Add("Por Productos");
            SetupTitle(wsProducts, "ESTADÍSTICAS POR PRODUCTOS", req.FromDate, req.ToDate, zone);

            var productGroups = orders
                .SelectMany(o => o.Items.Where(i => i.Status != "CANCELLED"))
                .GroupBy(i => new { Name = string.IsNullOrWhiteSpace(i.ProductName) ? (i.Product?.Name ?? "Sin nombre") : i.ProductName, Category = i.Product?.Category?.Name ?? "General" })
                .Select(g => new
                {
                    g.Key.Name,
                    g.Key.Category,
                    Quantity = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(p => p.Total)
                .ToList();

            var row = 4;
            SetHeaders(wsProducts, row, "Posición", "Producto", "Categoría", "Cantidad Vendida", "Total Venta ($)", "% del Total");
            var pos = 1;
            var grandTotal = productGroups.Sum(p => p.Total);
            foreach (var item in productGroups)
            {
                row++;
                wsProducts.Cell(row, 1).Value = pos++;
                wsProducts.Cell(row, 2).Value = item.Name;
                wsProducts.Cell(row, 3).Value = item.Category;
                wsProducts.Cell(row, 4).Value = item.Quantity;
                wsProducts.Cell(row, 5).Value = item.Total;
                wsProducts.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                wsProducts.Cell(row, 6).Value = grandTotal > 0 ? (item.Total / grandTotal) : 0m;
                wsProducts.Cell(row, 6).Style.NumberFormat.Format = "0.0%";
            }
            FormatDataRows(wsProducts, 5, row, 6);
            wsProducts.Columns().AdjustToContents();
        }

        if (includeTables)
        {
            var wsTables = wb.Worksheets.Add("Por Mesas");
            SetupTitle(wsTables, "ESTADÍSTICAS POR MESAS", req.FromDate, req.ToDate, zone);

            var tableGroups = orders
                .GroupBy(o => o.Table != null ? $"{o.Table.Name} ({o.Table.DiningArea?.Name ?? "Sin sala"})" : "Para Llevar / Caja")
                .Select(g => new
                {
                    TableName = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(o => (o.SubtotalAmount + (req.IncludeTips == true ? o.TipAmount : 0m))),
                    Tips = g.Sum(o => o.TipAmount)
                })
                .OrderByDescending(t => t.Total)
                .ToList();

            var row = 4;
            SetHeaders(wsTables, row, "Mesa / Ubicación", "Comandas Atendidas", "Total Vendido ($)", "Propinas ($)", "Ticket Promedio ($)");
            foreach (var t in tableGroups)
            {
                row++;
                wsTables.Cell(row, 1).Value = t.TableName;
                wsTables.Cell(row, 2).Value = t.Count;
                wsTables.Cell(row, 3).Value = t.Total;
                wsTables.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsTables.Cell(row, 4).Value = t.Tips;
                wsTables.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                var avg = t.Count > 0 ? t.Total / t.Count : 0m;
                wsTables.Cell(row, 5).Value = avg;
                wsTables.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
            }
            FormatDataRows(wsTables, 5, row, 5);
            wsTables.Columns().AdjustToContents();
        }

        if (includeWaiters)
        {
            var wsWaiters = wb.Worksheets.Add("Por Meseros");
            SetupTitle(wsWaiters, "ESTADÍSTICAS POR MESEROS / CAJEROS", req.FromDate, req.ToDate, zone);

            var waiterGroups = orders
                .GroupBy(o => string.IsNullOrWhiteSpace(o.CreatedByUserName) ? "No especificado" : o.CreatedByUserName)
                .Select(g => new
                {
                    Waiter = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(o => (o.SubtotalAmount + (req.IncludeTips == true ? o.TipAmount : 0m))),
                    Tips = g.Sum(o => o.TipAmount)
                })
                .OrderByDescending(w => w.Total)
                .ToList();

            var row = 4;
            SetHeaders(wsWaiters, row, "Mesero / Usuario", "Comandas Registradas", "Total Vendido ($)", "Propinas Acumuladas ($)");
            foreach (var w in waiterGroups)
            {
                row++;
                wsWaiters.Cell(row, 1).Value = w.Waiter;
                wsWaiters.Cell(row, 2).Value = w.Count;
                wsWaiters.Cell(row, 3).Value = w.Total;
                wsWaiters.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsWaiters.Cell(row, 4).Value = w.Tips;
                wsWaiters.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            }
            FormatDataRows(wsWaiters, 5, row, 4);
            wsWaiters.Columns().AdjustToContents();
        }

        if (includeCategories)
        {
            var wsCats = wb.Worksheets.Add("Por Categorías");
            SetupTitle(wsCats, "ESTADÍSTICAS POR CATEGORÍAS", req.FromDate, req.ToDate, zone);

            var catGroups = orders
                .SelectMany(o => o.Items.Where(i => i.Status != "CANCELLED"))
                .GroupBy(i => i.Product?.Category?.Name ?? "Sin Categoría")
                .Select(g => new
                {
                    Category = g.Key,
                    Quantity = g.Sum(i => i.Quantity),
                    Total = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(c => c.Total)
                .ToList();

            var row = 4;
            SetHeaders(wsCats, row, "Categoría", "Cantidad de Unidades", "Total Facturado ($)", "% de Ventas");
            var catGrandTotal = catGroups.Sum(c => c.Total);
            foreach (var c in catGroups)
            {
                row++;
                wsCats.Cell(row, 1).Value = c.Category;
                wsCats.Cell(row, 2).Value = c.Quantity;
                wsCats.Cell(row, 3).Value = c.Total;
                wsCats.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsCats.Cell(row, 4).Value = catGrandTotal > 0 ? (c.Total / catGrandTotal) : 0m;
                wsCats.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            }
            FormatDataRows(wsCats, 5, row, 4);
            wsCats.Columns().AdjustToContents();
        }
    }

    private async Task BuildExpensesReportAsync(
        XLWorkbook wb,
        Guid tenantId,
        CreateReportRequest req,
        string zone,
        CancellationToken ct)
    {
        var query = context.Expenses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= req.FromDate && e.ExpenseDate <= req.ToDate);

        if (string.Equals(req.CashFilter, "ONLY_CASH_OUT", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.IsCashOut);
        }
        else if (string.Equals(req.CashFilter, "NO_CASH_OUT", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => !e.IsCashOut);
        }

        var expenses = await query
            .Include(e => e.Supplier)
            .OrderBy(e => e.ExpenseDate)
            .ToListAsync(ct);

        var ws = wb.Worksheets.Add("Gastos");
        SetupTitle(ws, "REPORTE DETALLADO DE GASTOS", req.FromDate, req.ToDate, zone);

        var totalExpenses = expenses.Where(e => e.Status == "ACTIVE").Sum(e => e.Amount);
        var totalCashOut = expenses.Where(e => e.Status == "ACTIVE" && e.IsCashOut).Sum(e => e.Amount);
        var totalNonCash = totalExpenses - totalCashOut;

        AddKpiCard(ws, 4, 1, "Total Gastos Activos", totalExpenses, true);
        AddKpiCard(ws, 4, 4, "Salidos de Caja (Efectivo)", totalCashOut, true);
        AddKpiCard(ws, 4, 7, "Gastos Fuera de Caja", totalNonCash, true);

        var periods = GeneratePeriods(req.FromDate, req.ToDate, req.PeriodGrouping, zone);
        var hasPeriods = periods.Count > 0;

        if (hasPeriods)
        {
            var wsPeriod = wb.Worksheets.Add("Gastos por Período");
            SetupTitle(wsPeriod, "EVOLUCIÓN DE GASTOS POR PERÍODO", req.FromDate, req.ToDate, zone);
            SetHeaders(wsPeriod, 4, "Período", "Cant. Gastos", "Total Egresos ($)", "Salidas de Caja ($)", "Otros Fondos ($)", "% del Total");
            var r = 4;
            var activeExpenses = expenses.Where(e => e.Status == "ACTIVE").ToList();
            var grandTotal = activeExpenses.Sum(e => e.Amount);
            foreach (var p in periods)
            {
                r++;
                var pExp = activeExpenses.Where(e => e.ExpenseDate >= p.Start && e.ExpenseDate <= p.End).ToList();
                var pTotal = pExp.Sum(e => e.Amount);
                var pCash = pExp.Where(e => e.IsCashOut).Sum(e => e.Amount);
                var pOther = pTotal - pCash;

                wsPeriod.Cell(r, 1).Value = p.Label;
                wsPeriod.Cell(r, 2).Value = pExp.Count;
                wsPeriod.Cell(r, 3).Value = pTotal;
                wsPeriod.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 4).Value = pCash;
                wsPeriod.Cell(r, 4).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 5).Value = pOther;
                wsPeriod.Cell(r, 5).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 6).Value = grandTotal > 0 ? (pTotal / grandTotal) : 0m;
                wsPeriod.Cell(r, 6).Style.NumberFormat.Format = "0.0%";
            }
            r++;
            wsPeriod.Cell(r, 1).Value = "TOTAL:";
            wsPeriod.Cell(r, 1).Style.Font.Bold = true;
            wsPeriod.Cell(r, 2).Value = activeExpenses.Count;
            wsPeriod.Cell(r, 2).Style.Font.Bold = true;
            wsPeriod.Cell(r, 3).Value = grandTotal;
            wsPeriod.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 3).Style.Font.Bold = true;
            FormatDataRows(wsPeriod, 5, r - 1, 6);
            wsPeriod.Columns().AdjustToContents();
        }

        var row = 7;
        var colCount = hasPeriods ? 11 : 10;
        if (hasPeriods)
        {
            SetHeaders(ws, row, "Consecutivo", "Período", "Fecha y Hora", "Concepto / Nombre", "Descripción", "Proveedor", "Salió de Caja", "Medio de Pago", "Registrado Por", "Estado", "Monto ($)");
        }
        else
        {
            SetHeaders(ws, row, "Consecutivo", "Fecha y Hora", "Concepto / Nombre", "Descripción", "Proveedor", "Salió de Caja", "Medio de Pago", "Registrado Por", "Estado", "Monto ($)");
        }

        foreach (var exp in expenses)
        {
            row++;
            var c = 1;
            ws.Cell(row, c++).Value = $"#{exp.ConsecutiveNumber}";
            if (hasPeriods)
            {
                ws.Cell(row, c++).Value = GetPeriodLabel(exp.ExpenseDate, periods);
            }
            ws.Cell(row, c++).Value = timeZones.ConvertFromUtc(exp.ExpenseDate, zone).ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, c++).Value = exp.Name;
            ws.Cell(row, c++).Value = exp.Description ?? "-";
            ws.Cell(row, c++).Value = exp.Supplier?.Name ?? "Sin proveedor";
            ws.Cell(row, c++).Value = exp.IsCashOut ? "SÍ" : "NO";
            ws.Cell(row, c++).Value = exp.PaymentMethod;
            ws.Cell(row, c++).Value = exp.CreatedByUserName;
            ws.Cell(row, c++).Value = exp.Status == "ACTIVE" ? "Activo" : "Anulado";
            ws.Cell(row, c++).Value = exp.Amount;
            ws.Cell(row, c - 1).Style.NumberFormat.Format = "$#,##0.00";
            if (exp.Status != "ACTIVE")
            {
                ws.Row(row).Style.Font.FontColor = XLColor.FromHtml("#9CA3AF");
            }
        }

        row++;
        ws.Cell(row, colCount - 1).Value = "TOTAL ACTIVOS:";
        ws.Cell(row, colCount - 1).Style.Font.Bold = true;
        ws.Cell(row, colCount).Value = totalExpenses;
        ws.Cell(row, colCount).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, colCount).Style.Font.Bold = true;

        FormatDataRows(ws, 8, row - 1, colCount);
        ws.Columns().AdjustToContents();
    }

    private async Task BuildOrdersReportAsync(
        XLWorkbook wb,
        Guid tenantId,
        CreateReportRequest req,
        string zone,
        CancellationToken ct)
    {
        var orders = await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.CreatedAt >= req.FromDate && o.CreatedAt <= req.ToDate)
            .Include(o => o.Table)!.ThenInclude(t => t.DiningArea)
            .Include(o => o.Items)!.ThenInclude(i => i.Product)!.ThenInclude(p => p.Category)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(ct);

        var periods = GeneratePeriods(req.FromDate, req.ToDate, req.PeriodGrouping, zone);
        var hasPeriods = periods.Count > 0;

        if (hasPeriods)
        {
            var wsPeriod = wb.Worksheets.Add("Ventas por Período");
            SetupTitle(wsPeriod, "RESUMEN DE VENTAS POR PERÍODO", req.FromDate, req.ToDate, zone);
            SetHeaders(wsPeriod, 4, "Período", "Comandas Pagadas", "Subtotal ($)", "Descuento ($)", "Propina ($)", "Total Facturado ($)", "% del Total");
            var r = 4;
            var paid = orders.Where(o => o.Status == "PAID").ToList();
            var totalPaid = paid.Sum(o => o.TotalAmount);
            foreach (var p in periods)
            {
                r++;
                var pOrders = paid.Where(o => o.CreatedAt >= p.Start && o.CreatedAt <= p.End).ToList();
                var pSubtotal = pOrders.Sum(o => o.SubtotalAmount);
                var pDiscount = pOrders.Sum(o => o.DiscountAmount);
                var pTips = pOrders.Sum(o => o.TipAmount);
                var pTotal = pOrders.Sum(o => o.TotalAmount);

                wsPeriod.Cell(r, 1).Value = p.Label;
                wsPeriod.Cell(r, 2).Value = pOrders.Count;
                wsPeriod.Cell(r, 3).Value = pSubtotal;
                wsPeriod.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 4).Value = pDiscount;
                wsPeriod.Cell(r, 4).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 5).Value = pTips;
                wsPeriod.Cell(r, 5).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 6).Value = pTotal;
                wsPeriod.Cell(r, 6).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 7).Value = totalPaid > 0 ? (pTotal / totalPaid) : 0m;
                wsPeriod.Cell(r, 7).Style.NumberFormat.Format = "0.0%";
            }
            r++;
            wsPeriod.Cell(r, 1).Value = "TOTAL:";
            wsPeriod.Cell(r, 1).Style.Font.Bold = true;
            wsPeriod.Cell(r, 2).Value = paid.Count;
            wsPeriod.Cell(r, 2).Style.Font.Bold = true;
            wsPeriod.Cell(r, 6).Value = totalPaid;
            wsPeriod.Cell(r, 6).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 6).Style.Font.Bold = true;
            FormatDataRows(wsPeriod, 5, r - 1, 7);
            wsPeriod.Columns().AdjustToContents();
        }

        var isByProducts = string.Equals(req.GroupBy, "PRODUCTS", StringComparison.OrdinalIgnoreCase);

        if (isByProducts)
        {
            var ws = wb.Worksheets.Add("Ventas por Productos");
            SetupTitle(ws, "REPORTE DE VENTAS POR PRODUCTOS", req.FromDate, req.ToDate, zone);

            var items = orders
                .Where(o => o.Status == "PAID")
                .SelectMany(o => o.Items.Where(i => i.Status != "CANCELLED"))
                .GroupBy(i => new { Name = string.IsNullOrWhiteSpace(i.ProductName) ? (i.Product?.Name ?? "Sin nombre") : i.ProductName, Category = i.Product?.Category?.Name ?? "General" })
                .Select(g => new
                {
                    g.Key.Name,
                    g.Key.Category,
                    Quantity = g.Sum(x => x.Quantity),
                    TotalDiscount = g.Sum(x => x.DiscountAmount),
                    Total = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(p => p.Total)
                .ToList();

            var totalQty = items.Sum(i => i.Quantity);
            var totalSales = items.Sum(i => i.Total);

            AddKpiCard(ws, 4, 1, "Unidades Vendidas", totalQty, false);
            AddKpiCard(ws, 4, 4, "Total Ventas Facturadas", totalSales, true);

            var row = 7;
            SetHeaders(ws, row, "Producto", "Categoría", "Cantidad Vendida", "Precio Promedio ($)", "Descuento ($)", "Total Facturado ($)");

            foreach (var item in items)
            {
                row++;
                ws.Cell(row, 1).Value = item.Name;
                ws.Cell(row, 2).Value = item.Category;
                ws.Cell(row, 3).Value = item.Quantity;
                var avgPrice = item.Quantity > 0 ? (item.Total + item.TotalDiscount) / item.Quantity : 0m;
                ws.Cell(row, 4).Value = avgPrice;
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Value = item.TotalDiscount;
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Value = item.Total;
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
            }

            row++;
            ws.Cell(row, 5).Value = "TOTAL:";
            ws.Cell(row, 5).Style.Font.Bold = true;
            ws.Cell(row, 6).Value = totalSales;
            ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 6).Style.Font.Bold = true;

            FormatDataRows(ws, 8, row - 1, 6);
            ws.Columns().AdjustToContents();
        }
        else
        {
            var ws = wb.Worksheets.Add("Ventas por Comandas");
            SetupTitle(ws, "REPORTE DE VENTAS POR COMANDAS", req.FromDate, req.ToDate, zone);

            var paidOrders = orders.Where(o => o.Status == "PAID").ToList();
            var totalSales = paidOrders.Sum(o => o.TotalAmount);
            var totalTips = paidOrders.Sum(o => o.TipAmount);

            AddKpiCard(ws, 4, 1, "Total Comandas", orders.Count, false);
            AddKpiCard(ws, 4, 4, "Total Facturado", totalSales, true);
            AddKpiCard(ws, 4, 7, "Total Propinas", totalTips, true);

            var row = 7;
            var colCount = hasPeriods ? 13 : 12;
            if (hasPeriods)
            {
                SetHeaders(ws, row, "N° Orden", "Período", "Mesa / Salón", "Fecha y Hora", "Cant. Productos", "Subtotal ($)", "Descuento ($)", "Propina ($)", "Total ($)", "Medio de Pago", "Atendido Por", "Cobrado Por", "Estado");
            }
            else
            {
                SetHeaders(ws, row, "N° Orden", "Mesa / Salón", "Fecha y Hora", "Cant. Productos", "Subtotal ($)", "Descuento ($)", "Propina ($)", "Total ($)", "Medio de Pago", "Atendido Por", "Cobrado Por", "Estado");
            }

            foreach (var o in orders)
            {
                row++;
                var c = 1;
                ws.Cell(row, c++).Value = $"#{o.OrderNumber}";
                if (hasPeriods)
                {
                    ws.Cell(row, c++).Value = GetPeriodLabel(o.CreatedAt, periods);
                }
                ws.Cell(row, c++).Value = o.Table != null ? $"{o.Table.Name} ({o.Table.DiningArea?.Name ?? "-"})" : "Para Llevar / Caja";
                ws.Cell(row, c++).Value = timeZones.ConvertFromUtc(o.CreatedAt, zone).ToString("yyyy-MM-dd HH:mm");
                ws.Cell(row, c++).Value = o.Items.Where(i => i.Status != "CANCELLED").Sum(i => i.Quantity);
                ws.Cell(row, c++).Value = o.SubtotalAmount;
                ws.Cell(row, c++).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, c++).Value = o.DiscountAmount;
                ws.Cell(row, c++).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, c++).Value = o.TipAmount;
                ws.Cell(row, c++).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, c++).Value = o.TotalAmount;
                ws.Cell(row, c++).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, c++).Value = o.PaymentMethod ?? "Sin registrar";
                ws.Cell(row, c++).Value = o.CreatedByUserName;
                ws.Cell(row, c++).Value = o.PaidByUserName ?? "-";
                ws.Cell(row, c++).Value = o.Status == "PAID" ? "Pagada" : (o.Status == "CANCELLED" ? "Anulada" : "Pendiente");

                if (o.Status != "PAID")
                {
                    ws.Row(row).Style.Font.FontColor = XLColor.FromHtml("#9CA3AF");
                }
            }

            row++;
            var totalCol = hasPeriods ? 9 : 8;
            ws.Cell(row, totalCol - 1).Value = "TOTAL PAGADO:";
            ws.Cell(row, totalCol - 1).Style.Font.Bold = true;
            ws.Cell(row, totalCol).Value = totalSales;
            ws.Cell(row, totalCol).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, totalCol).Style.Font.Bold = true;

            FormatDataRows(ws, 8, row - 1, colCount);
            ws.Columns().AdjustToContents();
        }
    }

    private async Task BuildSalesAndExpensesReportAsync(
        XLWorkbook wb,
        Guid tenantId,
        CreateReportRequest req,
        string zone,
        CancellationToken ct)
    {
        var orders = await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.Status == "PAID" && o.CreatedAt >= req.FromDate && o.CreatedAt <= req.ToDate)
            .Include(o => o.Table)
            .Include(o => o.Items)
            .ToListAsync(ct);

        var expenses = await context.Expenses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.Status == "ACTIVE" && e.ExpenseDate >= req.FromDate && e.ExpenseDate <= req.ToDate)
            .Include(e => e.Supplier)
            .ToListAsync(ct);

        var ws = wb.Worksheets.Add("Flujo Ventas y Gastos");
        SetupTitle(ws, "REPORTE CONSOLIDADO DE VENTAS Y GASTOS", req.FromDate, req.ToDate, zone);

        var totalSales = orders.Sum(o => o.TotalAmount);
        var totalExpenses = expenses.Sum(e => e.Amount);
        var netBalance = totalSales - totalExpenses;

        AddKpiCard(ws, 4, 1, "Total Ventas (Ingresos)", totalSales, true);
        AddKpiCard(ws, 4, 4, "Total Gastos (Egresos)", totalExpenses, true);
        AddKpiCard(ws, 4, 7, "Balance Neto Operativo", netBalance, true);

        var periods = GeneratePeriods(req.FromDate, req.ToDate, req.PeriodGrouping, zone);
        var hasPeriods = periods.Count > 0;

        if (hasPeriods)
        {
            var wsPeriod = wb.Worksheets.Add("Balance por Período");
            SetupTitle(wsPeriod, "BALANCE DE VENTAS Y GASTOS POR PERÍODO", req.FromDate, req.ToDate, zone);
            SetHeaders(wsPeriod, 4, "Período", "Cant. Ventas", "Total Ventas ($)", "Cant. Gastos", "Total Gastos ($)", "Balance Neto ($)", "Margen Operativo");
            var r = 4;
            foreach (var p in periods)
            {
                r++;
                var pOrders = orders.Where(o => o.CreatedAt >= p.Start && o.CreatedAt <= p.End).ToList();
                var pExpenses = expenses.Where(e => e.ExpenseDate >= p.Start && e.ExpenseDate <= p.End).ToList();
                var pSalesTotal = pOrders.Sum(o => o.TotalAmount);
                var pExpTotal = pExpenses.Sum(e => e.Amount);
                var pNet = pSalesTotal - pExpTotal;
                var pMargin = pSalesTotal > 0 ? (pNet / pSalesTotal) : 0m;

                wsPeriod.Cell(r, 1).Value = p.Label;
                wsPeriod.Cell(r, 2).Value = pOrders.Count;
                wsPeriod.Cell(r, 3).Value = pSalesTotal;
                wsPeriod.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 4).Value = pExpenses.Count;
                wsPeriod.Cell(r, 5).Value = pExpTotal;
                wsPeriod.Cell(r, 5).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 6).Value = pNet;
                wsPeriod.Cell(r, 6).Style.NumberFormat.Format = "$#,##0.00";
                if (pNet < 0) wsPeriod.Cell(r, 6).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                wsPeriod.Cell(r, 7).Value = pMargin;
                wsPeriod.Cell(r, 7).Style.NumberFormat.Format = "0.0%";
            }
            r++;
            wsPeriod.Cell(r, 1).Value = "TOTAL:";
            wsPeriod.Cell(r, 1).Style.Font.Bold = true;
            wsPeriod.Cell(r, 2).Value = orders.Count;
            wsPeriod.Cell(r, 2).Style.Font.Bold = true;
            wsPeriod.Cell(r, 3).Value = totalSales;
            wsPeriod.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 3).Style.Font.Bold = true;
            wsPeriod.Cell(r, 4).Value = expenses.Count;
            wsPeriod.Cell(r, 4).Style.Font.Bold = true;
            wsPeriod.Cell(r, 5).Value = totalExpenses;
            wsPeriod.Cell(r, 5).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 5).Style.Font.Bold = true;
            wsPeriod.Cell(r, 6).Value = netBalance;
            wsPeriod.Cell(r, 6).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 6).Style.Font.Bold = true;
            wsPeriod.Cell(r, 7).Value = totalSales > 0 ? (netBalance / totalSales) : 0m;
            wsPeriod.Cell(r, 7).Style.NumberFormat.Format = "0.0%";
            wsPeriod.Cell(r, 7).Style.Font.Bold = true;

            FormatDataRows(wsPeriod, 5, r - 1, 7);
            wsPeriod.Columns().AdjustToContents();
        }

        // Combined chronological transactions
        var transactions = new List<(DateTimeOffset Date, string Type, string Ref, string Concept, string PaymentMethod, string User, decimal Income, decimal Expense)>();

        var isByProducts = string.Equals(req.GroupBy, "PRODUCTS", StringComparison.OrdinalIgnoreCase);

        if (isByProducts)
        {
            foreach (var order in orders)
            {
                foreach (var item in order.Items.Where(i => i.Status != "CANCELLED"))
                {
                    transactions.Add((
                        order.CreatedAt,
                        "Ingreso",
                        $"Comanda #{order.OrderNumber}",
                        $"{item.ProductName} (x{item.Quantity})",
                        order.PaymentMethod ?? "Efectivo",
                        order.CreatedByUserName,
                        item.Subtotal,
                        0m
                    ));
                }
            }
        }
        else
        {
            foreach (var order in orders)
            {
                var itemCount = order.Items.Where(i => i.Status != "CANCELLED").Sum(i => i.Quantity);
                var tableName = order.Table != null ? order.Table.Name : "Caja / Llevar";
                transactions.Add((
                    order.CreatedAt,
                    "Ingreso",
                    $"Comanda #{order.OrderNumber}",
                    $"Venta Mesa {tableName} ({itemCount} productos)",
                    order.PaymentMethod ?? "Efectivo",
                    order.CreatedByUserName,
                    order.TotalAmount,
                    0m
                ));
            }
        }

        foreach (var exp in expenses)
        {
            transactions.Add((
                exp.ExpenseDate,
                "Gasto",
                $"Gasto #{exp.ConsecutiveNumber}",
                $"{exp.Name} - {exp.Supplier?.Name ?? "General"}",
                exp.PaymentMethod,
                exp.CreatedByUserName,
                0m,
                exp.Amount
            ));
        }

        transactions = transactions.OrderBy(t => t.Date).ToList();

        var row = 7;
        var colCount = hasPeriods ? 10 : 9;
        if (hasPeriods)
        {
            SetHeaders(ws, row, "Tipo", "Período", "Fecha y Hora", "N° Referencia", "Concepto / Detalle", "Medio de Pago", "Responsable", "Ingreso ($)", "Gasto ($)", "Saldo Acumulado ($)");
        }
        else
        {
            SetHeaders(ws, row, "Tipo", "Fecha y Hora", "N° Referencia", "Concepto / Detalle", "Medio de Pago", "Responsable", "Ingreso ($)", "Gasto ($)", "Saldo Acumulado ($)");
        }

        var runningBalance = 0m;
        foreach (var tx in transactions)
        {
            row++;
            runningBalance += (tx.Income - tx.Expense);

            var c = 1;
            var typeCell = ws.Cell(row, c++);
            typeCell.Value = tx.Type;
            if (tx.Type == "Ingreso")
            {
                typeCell.Style.Font.FontColor = XLColor.FromHtml("#16A34A");
                typeCell.Style.Font.Bold = true;
            }
            else
            {
                typeCell.Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                typeCell.Style.Font.Bold = true;
            }

            if (hasPeriods)
            {
                ws.Cell(row, c++).Value = GetPeriodLabel(tx.Date, periods);
            }

            ws.Cell(row, c++).Value = timeZones.ConvertFromUtc(tx.Date, zone).ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, c++).Value = tx.Ref;
            ws.Cell(row, c++).Value = tx.Concept;
            ws.Cell(row, c++).Value = tx.PaymentMethod;
            ws.Cell(row, c++).Value = tx.User;
            ws.Cell(row, c++).Value = tx.Income;
            ws.Cell(row, c - 1).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, c++).Value = tx.Expense;
            ws.Cell(row, c - 1).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, c++).Value = runningBalance;
            ws.Cell(row, c - 1).Style.NumberFormat.Format = "$#,##0.00";
        }

        row++;
        var labelCol = hasPeriods ? 7 : 6;
        ws.Cell(row, labelCol).Value = "TOTALES:";
        ws.Cell(row, labelCol).Style.Font.Bold = true;
        ws.Cell(row, labelCol + 1).Value = totalSales;
        ws.Cell(row, labelCol + 1).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, labelCol + 1).Style.Font.Bold = true;
        ws.Cell(row, labelCol + 2).Value = totalExpenses;
        ws.Cell(row, labelCol + 2).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, labelCol + 2).Style.Font.Bold = true;
        ws.Cell(row, labelCol + 3).Value = netBalance;
        ws.Cell(row, labelCol + 3).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, labelCol + 3).Style.Font.Bold = true;

        FormatDataRows(ws, 8, row - 1, colCount);
        ws.Columns().AdjustToContents();
    }

    private async Task BuildCashRegistersReportAsync(
        XLWorkbook wb,
        Guid tenantId,
        CreateReportRequest req,
        string zone,
        CancellationToken ct)
    {
        var shifts = await context.CashRegisterShifts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.OpenedAt >= req.FromDate && s.OpenedAt <= req.ToDate)
            .Include(s => s.CashRegister)
            .OrderBy(s => s.OpenedAt)
            .ToListAsync(ct);

        var orders = await context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.Status == "PAID" && o.CreatedAt >= req.FromDate && o.CreatedAt <= req.ToDate)
            .ToListAsync(ct);

        var expenses = await context.Expenses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.Status == "ACTIVE" && e.ExpenseDate >= req.FromDate && e.ExpenseDate <= req.ToDate)
            .ToListAsync(ct);

        var periods = GeneratePeriods(req.FromDate, req.ToDate, req.PeriodGrouping, zone);
        var hasPeriods = periods.Count > 0;

        if (hasPeriods)
        {
            var wsPeriod = wb.Worksheets.Add("Cajas por Período");
            SetupTitle(wsPeriod, "ACTIVIDAD DE CAJAS POR PERÍODO", req.FromDate, req.ToDate, zone);
            SetHeaders(wsPeriod, 4, "Período", "Turnos Cerrados", "Ventas en Caja ($)", "Gastos en Caja ($)", "Flujo Neto ($)");
            var r = 4;
            foreach (var p in periods)
            {
                r++;
                var pShifts = shifts.Where(s => s.OpenedAt >= p.Start && s.OpenedAt <= p.End).ToList();
                var pSales = pShifts.Sum(s => s.TotalSalesAmount ?? 0m);
                var pExp = pShifts.Sum(s => s.TotalExpensesAmount ?? 0m);
                var pNet = pSales - pExp;

                wsPeriod.Cell(r, 1).Value = p.Label;
                wsPeriod.Cell(r, 2).Value = pShifts.Count(s => s.Status == "CLOSED");
                wsPeriod.Cell(r, 3).Value = pSales;
                wsPeriod.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 4).Value = pExp;
                wsPeriod.Cell(r, 4).Style.NumberFormat.Format = "$#,##0.00";
                wsPeriod.Cell(r, 5).Value = pNet;
                wsPeriod.Cell(r, 5).Style.NumberFormat.Format = "$#,##0.00";
            }
            r++;
            wsPeriod.Cell(r, 1).Value = "TOTAL:";
            wsPeriod.Cell(r, 1).Style.Font.Bold = true;
            wsPeriod.Cell(r, 2).Value = shifts.Count(s => s.Status == "CLOSED");
            wsPeriod.Cell(r, 2).Style.Font.Bold = true;
            var totalShiftSales = shifts.Sum(s => s.TotalSalesAmount ?? 0m);
            var totalShiftExp = shifts.Sum(s => s.TotalExpensesAmount ?? 0m);
            wsPeriod.Cell(r, 3).Value = totalShiftSales;
            wsPeriod.Cell(r, 3).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 3).Style.Font.Bold = true;
            wsPeriod.Cell(r, 4).Value = totalShiftExp;
            wsPeriod.Cell(r, 4).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 4).Style.Font.Bold = true;
            wsPeriod.Cell(r, 5).Value = totalShiftSales - totalShiftExp;
            wsPeriod.Cell(r, 5).Style.NumberFormat.Format = "$#,##0.00";
            wsPeriod.Cell(r, 5).Style.Font.Bold = true;

            FormatDataRows(wsPeriod, 5, r - 1, 5);
            wsPeriod.Columns().AdjustToContents();
        }

        var wsSummary = wb.Worksheets.Add("Resumen de Cajas");
        SetupTitle(wsSummary, "REPORTE GENERAL DE CAJAS Y MEDIOS DE PAGO", req.FromDate, req.ToDate, zone);

        var totalSales = orders.Sum(o => o.TotalAmount);
        var totalExpenses = expenses.Sum(e => e.Amount);
        var netCash = totalSales - totalExpenses;

        AddKpiCard(wsSummary, 4, 1, "Ventas Totales Registradas", totalSales, true);
        AddKpiCard(wsSummary, 4, 4, "Gastos Totales Registrados", totalExpenses, true);
        AddKpiCard(wsSummary, 4, 7, "Flujo Neto General", netCash, true);

        // Desglose por Medios de Pago
        var paymentMap = new Dictionary<string, (decimal Sales, decimal Expenses)>(StringComparer.OrdinalIgnoreCase);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var order in orders)
        {
            var splits = ExtractPaymentSplits(order.PaymentDetailsJson, jsonOptions);
            if (splits != null && splits.Count > 0)
            {
                foreach (var split in splits)
                {
                    var method = string.IsNullOrWhiteSpace(split.Method) ? "Efectivo" : split.Method.Trim();
                    var current = paymentMap.GetValueOrDefault(method);
                    paymentMap[method] = (current.Sales + split.Amount, current.Expenses);
                }
            }
            else
            {
                var method = string.IsNullOrWhiteSpace(order.PaymentMethod) ? "Efectivo" : order.PaymentMethod.Trim();
                var current = paymentMap.GetValueOrDefault(method);
                paymentMap[method] = (current.Sales + order.TotalAmount, current.Expenses);
            }
        }

        foreach (var exp in expenses)
        {
            var method = string.IsNullOrWhiteSpace(exp.PaymentMethod) ? "Efectivo" : exp.PaymentMethod.Trim();
            var current = paymentMap.GetValueOrDefault(method);
            paymentMap[method] = (current.Sales, current.Expenses + exp.Amount);
        }

        var row = 7;
        SetHeaders(wsSummary, row, "Medio de Pago", "Total Ventas ($)", "Total Gastos ($)", "Balance Neto ($)");
        foreach (var kv in paymentMap.OrderByDescending(p => p.Value.Sales))
        {
            row++;
            wsSummary.Cell(row, 1).Value = kv.Key;
            wsSummary.Cell(row, 2).Value = kv.Value.Sales;
            wsSummary.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
            wsSummary.Cell(row, 3).Value = kv.Value.Expenses;
            wsSummary.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
            var net = kv.Value.Sales - kv.Value.Expenses;
            wsSummary.Cell(row, 4).Value = net;
            wsSummary.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
        }
        row++;
        wsSummary.Cell(row, 1).Value = "TOTAL:";
        wsSummary.Cell(row, 1).Style.Font.Bold = true;
        wsSummary.Cell(row, 2).Value = totalSales;
        wsSummary.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
        wsSummary.Cell(row, 2).Style.Font.Bold = true;
        wsSummary.Cell(row, 3).Value = totalExpenses;
        wsSummary.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
        wsSummary.Cell(row, 3).Style.Font.Bold = true;
        wsSummary.Cell(row, 4).Value = netCash;
        wsSummary.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
        wsSummary.Cell(row, 4).Style.Font.Bold = true;

        FormatDataRows(wsSummary, 8, row - 1, 4);
        wsSummary.Columns().AdjustToContents();

        // Sheet 2: Detalle de Turnos
        var wsShifts = wb.Worksheets.Add("Turnos de Caja");
        SetupTitle(wsShifts, "DETALLE DE TURNOS Y ARQUEOS DE CAJA", req.FromDate, req.ToDate, zone);

        var sRow = 4;
        var sColCount = hasPeriods ? 11 : 10;
        if (hasPeriods)
        {
            SetHeaders(wsShifts, sRow, "Caja", "Período", "Apertura (Fecha)", "Cajero Apertura", "Fondo Inicial ($)", "Ventas Totales ($)", "Gastos Turno ($)", "Cierre (Fecha)", "Cajero Cierre", "Total Recaudado ($)", "Estado");
        }
        else
        {
            SetHeaders(wsShifts, sRow, "Caja", "Apertura (Fecha)", "Cajero Apertura", "Fondo Inicial ($)", "Ventas Totales ($)", "Gastos Turno ($)", "Cierre (Fecha)", "Cajero Cierre", "Total Recaudado ($)", "Estado");
        }

        foreach (var s in shifts)
        {
            sRow++;
            var c = 1;
            wsShifts.Cell(sRow, c++).Value = s.CashRegister?.Name ?? "Caja Principal";
            if (hasPeriods)
            {
                wsShifts.Cell(sRow, c++).Value = GetPeriodLabel(s.OpenedAt, periods);
            }
            wsShifts.Cell(sRow, c++).Value = timeZones.ConvertFromUtc(s.OpenedAt, zone).ToString("yyyy-MM-dd HH:mm");
            wsShifts.Cell(sRow, c++).Value = s.OpenedByUserName;

            var initialBalance = ExtractOpeningBalance(s.OpeningBalancesJson);
            wsShifts.Cell(sRow, c++).Value = initialBalance;
            wsShifts.Cell(sRow, c - 1).Style.NumberFormat.Format = "$#,##0.00";

            wsShifts.Cell(sRow, c++).Value = s.TotalSalesAmount ?? 0m;
            wsShifts.Cell(sRow, c - 1).Style.NumberFormat.Format = "$#,##0.00";

            wsShifts.Cell(sRow, c++).Value = s.TotalExpensesAmount ?? 0m;
            wsShifts.Cell(sRow, c - 1).Style.NumberFormat.Format = "$#,##0.00";

            wsShifts.Cell(sRow, c++).Value = s.ClosedAt.HasValue ? timeZones.ConvertFromUtc(s.ClosedAt.Value, zone).ToString("yyyy-MM-dd HH:mm") : "En curso (Abierto)";
            wsShifts.Cell(sRow, c++).Value = s.ClosedByUserName ?? "-";

            wsShifts.Cell(sRow, c++).Value = s.TotalCollectedAmount ?? 0m;
            wsShifts.Cell(sRow, c - 1).Style.NumberFormat.Format = "$#,##0.00";

            wsShifts.Cell(sRow, c++).Value = s.Status == "OPEN" ? "Abierto" : "Cerrado";
        }

        FormatDataRows(wsShifts, 5, sRow, sColCount);
        wsShifts.Columns().AdjustToContents();
    }

    private static decimal ExtractOpeningBalance(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0m;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var sum = 0m;
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("amount", out var amountProp) || el.TryGetProperty("Amount", out amountProp))
                    {
                        sum += amountProp.GetDecimal();
                    }
                }
                return sum;
            }
        }
        catch { }
        return 0m;
    }

    private sealed record PaymentSplitItem(string? Method, decimal Amount);

    private static List<PaymentSplitItem>? ExtractPaymentSplits(string? json, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<PaymentSplitItem>>(json, options);
        }
        catch
        {
            return null;
        }
    }

    private void SetupTitle(IXLWorksheet ws, string title, DateTimeOffset from, DateTimeOffset to, string zone)
    {
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        titleCell.Style.Font.FontColor = PrimaryHeaderBg;

        var subCell = ws.Cell(2, 1);
        subCell.Value = $"Período: {timeZones.ConvertFromUtc(from, zone):yyyy-MM-dd HH:mm} hasta {timeZones.ConvertFromUtc(to, zone):yyyy-MM-dd HH:mm} | Generado: {timeZones.ConvertFromUtc(clock.UtcNow, zone):yyyy-MM-dd HH:mm}";
        subCell.Style.Font.FontSize = 10;
        subCell.Style.Font.FontColor = XLColor.FromHtml("#6B7280");
    }

    private static void AddKpiCard(IXLWorksheet ws, int row, int col, string label, decimal value, bool isCurrency)
    {
        var range = ws.Range(row, col, row + 1, col + 1);
        range.Style.Fill.BackgroundColor = SubtleBg;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = BorderColor;

        var lblCell = ws.Cell(row, col);
        lblCell.Value = label;
        lblCell.Style.Font.FontSize = 9;
        lblCell.Style.Font.FontColor = XLColor.FromHtml("#4B5563");

        var valCell = ws.Cell(row + 1, col);
        valCell.Value = value;
        valCell.Style.Font.Bold = true;
        valCell.Style.Font.FontSize = 13;
        valCell.Style.Font.FontColor = PrimaryHeaderBg;
        if (isCurrency) valCell.Style.NumberFormat.Format = "$#,##0.00";
        else valCell.Style.NumberFormat.Format = "#,##0";
    }

    private static void SetHeaders(IXLWorksheet ws, int row, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 10;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = SecondaryHeaderBg;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderColor;
        }
        ws.Row(row).Height = 24;
    }

    private static void FormatDataRows(IXLWorksheet ws, int startRow, int endRow, int colCount)
    {
        if (endRow < startRow) return;
        var range = ws.Range(startRow, 1, endRow, colCount);
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorderColor = BorderColor;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = BorderColor;
        range.Style.Font.FontSize = 10;

        for (var r = startRow; r <= endRow; r++)
        {
            if ((r - startRow) % 2 == 1)
            {
                ws.Range(r, 1, r, colCount).Style.Fill.BackgroundColor = SubtleBg;
            }
        }
    }

    private sealed record ReportPeriod(string Label, DateTimeOffset Start, DateTimeOffset End);

    private List<ReportPeriod> GeneratePeriods(DateTimeOffset fromDate, DateTimeOffset toDate, string? periodGrouping, string zone)
    {
        var periods = new List<ReportPeriod>();
        if (string.IsNullOrWhiteSpace(periodGrouping) || periodGrouping == "NONE")
        {
            return periods;
        }

        var isWeekly = string.Equals(periodGrouping, "WEEKLY", StringComparison.OrdinalIgnoreCase);
        var isMonthly = string.Equals(periodGrouping, "MONTHLY", StringComparison.OrdinalIgnoreCase);

        if (!isWeekly && !isMonthly)
        {
            return periods;
        }

        var culture = new System.Globalization.CultureInfo("es-CO");
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;

        var localStart = timeZones.ConvertFromUtc(fromDate, zone);
        var localEnd = timeZones.ConvertFromUtc(toDate, zone);

        if (localStart > localEnd)
        {
            return periods;
        }

        if (isWeekly)
        {
            var currentLocalDate = localStart.Date;
            var endDate = localEnd.Date;

            while (currentLocalDate <= endDate && periods.Count < 500)
            {
                var dayOfWeek = (int)currentLocalDate.DayOfWeek;
                var daysUntilSunday = (7 - (dayOfWeek == 0 ? 7 : dayOfWeek)) % 7;
                var weekEndDate = currentLocalDate.AddDays(daysUntilSunday);

                var pStartLocal = currentLocalDate == localStart.Date
                    ? localStart
                    : new DateTimeOffset(currentLocalDate.Year, currentLocalDate.Month, currentLocalDate.Day, 0, 0, 0, localStart.Offset);

                var pEndLocal = weekEndDate >= endDate
                    ? localEnd
                    : new DateTimeOffset(weekEndDate.Year, weekEndDate.Month, weekEndDate.Day, 23, 59, 59, localEnd.Offset).AddSeconds(1).AddTicks(-1);

                var weekNum = cal.GetWeekOfYear(currentLocalDate, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                var label = $"Semana {weekNum} ({currentLocalDate:dd/MM} - {pEndLocal:dd/MM})";

                periods.Add(new ReportPeriod(label, timeZones.ConvertToUtc(pStartLocal.DateTime, zone), timeZones.ConvertToUtc(pEndLocal.DateTime, zone)));

                var nextDate = currentLocalDate.AddDays(daysUntilSunday + 1);
                if (nextDate <= currentLocalDate)
                {
                    break;
                }
                currentLocalDate = nextDate;
            }
        }
        else if (isMonthly)
        {
            var currentLocalDate = new DateTime(localStart.Year, localStart.Month, 1);
            var endDate = localEnd.Date;

            while (currentLocalDate <= endDate && periods.Count < 500)
            {
                var daysInMonth = DateTime.DaysInMonth(currentLocalDate.Year, currentLocalDate.Month);
                var monthEndDate = new DateTime(currentLocalDate.Year, currentLocalDate.Month, daysInMonth);

                var pStartLocal = (currentLocalDate.Year == localStart.Year && currentLocalDate.Month == localStart.Month)
                    ? localStart
                    : new DateTimeOffset(currentLocalDate.Year, currentLocalDate.Month, 1, 0, 0, 0, localStart.Offset);

                var pEndLocal = (monthEndDate >= endDate)
                    ? localEnd
                    : new DateTimeOffset(currentLocalDate.Year, currentLocalDate.Month, daysInMonth, 23, 59, 59, localEnd.Offset).AddSeconds(1).AddTicks(-1);

                var rawMonthName = currentLocalDate.ToString("MMMM yyyy", culture);
                var monthName = char.ToUpperInvariant(rawMonthName[0]) + rawMonthName[1..];
                var label = $"{monthName} ({pStartLocal:dd/MM} - {pEndLocal:dd/MM})";

                periods.Add(new ReportPeriod(label, timeZones.ConvertToUtc(pStartLocal.DateTime, zone), timeZones.ConvertToUtc(pEndLocal.DateTime, zone)));

                var nextMonth = currentLocalDate.AddMonths(1);
                if (nextMonth <= currentLocalDate)
                {
                    break;
                }
                currentLocalDate = nextMonth;
            }
        }

        return periods;
    }

    private static string GetPeriodLabel(DateTimeOffset date, IReadOnlyList<ReportPeriod> periods)
    {
        if (periods.Count == 0) return "-";
        var found = periods.FirstOrDefault(p => date >= p.Start && date <= p.End);
        return found?.Label ?? "-";
    }
}

