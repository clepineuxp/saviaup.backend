using System.Text.Json;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.CashRegisters;

public sealed class OpenCashRegisterShiftUseCase(
    ICashRegisterRepository registerRepository,
    ICashRegisterShiftRepository shiftRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IOpenCashRegisterShiftUseCase
{
    public async Task<Result<CashRegisterShiftDto>> ExecuteAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        OpenCashRegisterShiftRequest request,
        CancellationToken cancellationToken)
    {
        var register = await registerRepository.GetByIdAsync(tenantId, request.CashRegisterId, cancellationToken);
        if (register is null || !register.IsActive)
            return Result<CashRegisterShiftDto>.Failure(Errors.CashRegisterNotFound);

        var existingOpen = await shiftRepository.GetOpenShiftAsync(tenantId, request.CashRegisterId, cancellationToken);
        if (existingOpen is not null)
            return Result<CashRegisterShiftDto>.Failure(Errors.CashRegisterAlreadyHasOpenShift);

        var initialBalances = request.InitialBalances ?? Array.Empty<OpeningBalanceInputDto>();
        var now = clock.UtcNow;

        var shift = new CashRegisterShift
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CashRegisterId = register.Id,
            CashRegister = register,
            Status = "OPEN",
            OpenedByUserId = userId,
            OpenedByUserName = userName,
            OpenedAt = now,
            OpeningBalancesJson = JsonSerializer.Serialize(initialBalances)
        };

        await shiftRepository.AddAsync(shift, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CashRegisterShiftDto>.Success(new CashRegisterShiftDto(
            shift.Id,
            shift.CashRegisterId,
            register.Name,
            shift.Status,
            shift.OpenedByUserId,
            shift.OpenedByUserName,
            shift.OpenedAt,
            shift.ClosedByUserId,
            shift.ClosedByUserName,
            shift.ClosedAt,
            0, 0, 0, 0,
            shift.OpeningBalancesJson,
            shift.ClosingSummaryJson));
    }
}

public sealed class CloseCashRegisterShiftUseCase(
    ICashRegisterShiftRepository shiftRepository,
    IOrderRepository orderRepository,
    ISettingsRepository settingsRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICloseCashRegisterShiftUseCase
{
    public async Task<Result<CashRegisterShiftDto>> ExecuteAsync(
        Guid tenantId,
        Guid shiftId,
        Guid userId,
        string userName,
        CloseCashRegisterShiftRequest request,
        CancellationToken cancellationToken)
    {
        var shift = await shiftRepository.GetByIdAsync(tenantId, shiftId, cancellationToken);
        if (shift is null)
            return Result<CashRegisterShiftDto>.Failure(Errors.CashRegisterShiftNotFound);

        if (shift.Status == "CLOSED")
            return Result<CashRegisterShiftDto>.Success(new CashRegisterShiftDto(
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
                shift.ClosingSummaryJson));

        // Requirement 4: Must have all tables closed and all orders paid before closing cash register
        if (await shiftRepository.HasOccupiedTablesOrPendingOrdersAsync(tenantId, cancellationToken))
        {
            return Result<CashRegisterShiftDto>.Failure(Errors.OccupiedTablesOrPendingOrdersPreventCashRegisterClose);
        }

        var summaryResult = await CalculateShiftSummaryAsync(tenantId, shift, request.ClosingBalances, orderRepository, settingsRepository, cancellationToken);

        var now = clock.UtcNow;
        shift.Status = "CLOSED";
        shift.ClosedByUserId = userId;
        shift.ClosedByUserName = userName;
        shift.ClosedAt = now;

        shift.TotalSalesAmount = summaryResult.TotalSalesAmount;
        shift.TotalTipsAmount = summaryResult.TotalTipsAmount;
        shift.TotalCollectedAmount = summaryResult.TotalCollectedAmount;
        shift.TotalExpensesAmount = summaryResult.TotalExpensesAmount;
        shift.ClosingSummaryJson = JsonSerializer.Serialize(summaryResult);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CashRegisterShiftDto>.Success(new CashRegisterShiftDto(
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
            shift.ClosingSummaryJson));
    }

    internal static async Task<CashRegisterShiftSummaryDto> CalculateShiftSummaryAsync(
        Guid tenantId,
        CashRegisterShift shift,
        IReadOnlyCollection<ClosingBalanceInputDto>? actualBalances,
        IOrderRepository orderRepository,
        ISettingsRepository settingsRepository,
        CancellationToken cancellationToken)
    {
        var ordersPage = await orderRepository.GetOrdersPageAsync(tenantId, new OrderQueryRequest
        {
            Page = 1,
            PageSize = 10000,
            Statuses = new[] { "PAID" },
            FromDate = shift.OpenedAt,
            ToDate = shift.ClosedAt ?? DateTimeOffset.UtcNow
        }, cancellationToken);

        var paidOrders = ordersPage.Items
            .Where(o => o.Status == "PAID")
            .ToArray();

        var totalSales = paidOrders.Sum(o => o.SubtotalAmount);
        var totalTips = paidOrders.Sum(o => o.TipAmount);
        var totalCollected = paidOrders.Sum(o => o.TotalAmount);
        var totalExpenses = 0m;

        // Parse initial opening balances
        List<OpeningBalanceInputDto> initialList = new();
        if (!string.IsNullOrWhiteSpace(shift.OpeningBalancesJson))
        {
            try
            {
                initialList = JsonSerializer.Deserialize<List<OpeningBalanceInputDto>>(shift.OpeningBalancesJson) ?? new();
            }
            catch { }
        }

        var configuredMethods = await settingsRepository.GetPaymentMethodsAsync(tenantId, includeInactive: true, cancellationToken);
        var actualMap = (actualBalances ?? Array.Empty<ClosingBalanceInputDto>())
            .ToDictionary(b => b.MethodName.Trim(), b => b.ActualAmount, StringComparer.OrdinalIgnoreCase);

        var methodSalesMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var methodTipsMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var pm in configuredMethods)
        {
            methodSalesMap[pm.Name.Trim()] = 0m;
            methodTipsMap[pm.Name.Trim()] = 0m;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var order in paidOrders)
        {
            List<PaymentSplitDto>? splits = null;
            if (!string.IsNullOrWhiteSpace(order.PaymentDetailsJson))
            {
                try
                {
                    splits = JsonSerializer.Deserialize<List<PaymentSplitDto>>(order.PaymentDetailsJson, jsonOptions);
                }
                catch { }
            }

            if (splits is not null && splits.Count > 0)
            {
                var orderTotal = order.TotalAmount > 0 ? order.TotalAmount : splits.Sum(s => s.Amount);
                var tipRatio = orderTotal > 0 ? (order.TipAmount / orderTotal) : 0m;

                foreach (var split in splits)
                {
                    if (string.IsNullOrWhiteSpace(split.Method) || split.Amount <= 0) continue;
                    var methodName = split.Method.Trim();

                    var splitTip = Math.Round(split.Amount * tipRatio, 2, MidpointRounding.AwayFromZero);
                    var splitSales = split.Amount - splitTip;

                    methodSalesMap[methodName] = methodSalesMap.GetValueOrDefault(methodName) + splitSales;
                    methodTipsMap[methodName] = methodTipsMap.GetValueOrDefault(methodName) + splitTip;
                }
            }
            else
            {
                var methodName = (order.PaymentMethod ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(methodName))
                {
                    methodSalesMap[methodName] = methodSalesMap.GetValueOrDefault(methodName) + order.SubtotalAmount;
                    methodTipsMap[methodName] = methodTipsMap.GetValueOrDefault(methodName) + order.TipAmount;
                }
            }
        }

        var methodSummaries = new List<PaymentMethodClosingSummaryDto>();

        foreach (var pm in configuredMethods)
        {
            var methodName = pm.Name.Trim();
            var initialAmt = initialList.FirstOrDefault(i => string.Equals(i.MethodName, methodName, StringComparison.OrdinalIgnoreCase))?.Amount ?? 0m;
            var salesAmt = methodSalesMap.GetValueOrDefault(methodName);
            var tipsAmt = methodTipsMap.GetValueOrDefault(methodName);
            var expensesAmt = 0m;
            var totalCollectedForMethod = salesAmt + tipsAmt;
            var expectedAmt = initialAmt + totalCollectedForMethod - expensesAmt;
            var actualAmt = actualMap.TryGetValue(methodName, out var aVal) ? aVal : expectedAmt;
            var diff = actualAmt - expectedAmt;

            methodSummaries.Add(new PaymentMethodClosingSummaryDto(
                methodName,
                initialAmt,
                salesAmt,
                tipsAmt,
                expensesAmt,
                totalCollectedForMethod,
                expectedAmt,
                actualAmt,
                diff));
        }

        return new CashRegisterShiftSummaryDto(
            shift.Id,
            shift.CashRegisterId,
            shift.CashRegister?.Name ?? "Caja",
            shift.Status,
            shift.OpenedByUserName,
            shift.OpenedAt,
            totalSales,
            totalTips,
            totalCollected,
            totalExpenses,
            methodSummaries);
    }
}

public sealed class GetCashRegisterShiftSummaryUseCase(
    ICashRegisterShiftRepository shiftRepository,
    IOrderRepository orderRepository,
    ISettingsRepository settingsRepository) : IGetCashRegisterShiftSummaryUseCase
{
    public async Task<Result<CashRegisterShiftSummaryDto>> ExecuteAsync(
        Guid tenantId,
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        var shift = await shiftRepository.GetByIdAsync(tenantId, shiftId, cancellationToken);
        if (shift is null)
            return Result<CashRegisterShiftSummaryDto>.Failure(Errors.CashRegisterShiftNotFound);

        if (!string.IsNullOrWhiteSpace(shift.ClosingSummaryJson))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<CashRegisterShiftSummaryDto>(shift.ClosingSummaryJson);
                if (cached is not null) return Result<CashRegisterShiftSummaryDto>.Success(cached);
            }
            catch { }
        }

        var summary = await CloseCashRegisterShiftUseCase.CalculateShiftSummaryAsync(
            tenantId, shift, null, orderRepository, settingsRepository, cancellationToken);

        return Result<CashRegisterShiftSummaryDto>.Success(summary);
    }
}

public sealed class ListCashRegisterShiftsUseCase(ICashRegisterShiftRepository shiftRepository) : IListCashRegisterShiftsUseCase
{
    public async Task<Result<PagedResponse<CashRegisterShiftDto>>> ExecuteAsync(
        Guid tenantId,
        CashRegisterShiftQueryRequest request,
        CancellationToken cancellationToken)
    {
        var page = await shiftRepository.GetShiftsPageAsync(tenantId, request, cancellationToken);
        var pageNumber = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : request.PageSize;
        var totalPages = page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)pageSize);

        var pagedResponse = new PagedResponse<CashRegisterShiftDto>(
            page.Items,
            pageNumber,
            pageSize,
            page.TotalCount,
            totalPages);

        return Result<PagedResponse<CashRegisterShiftDto>>.Success(pagedResponse);
    }
}
