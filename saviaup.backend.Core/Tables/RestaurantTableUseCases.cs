using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Tables;

public sealed class ListRestaurantTablesUseCase(IRestaurantTableRepository repository) : IListRestaurantTablesUseCase
{
    public async Task<Result<IReadOnlyCollection<RestaurantTableDto>>> ExecuteAsync(
        Guid tenantId,
        Guid? areaId,
        CancellationToken cancellationToken)
    {
        var tables = await repository.GetForTenantAsync(tenantId, areaId, cancellationToken);
        return Result<IReadOnlyCollection<RestaurantTableDto>>.Success(tables.Select(TableRules.ToDto).ToArray());
    }
}

public sealed class CreateRestaurantTableUseCase(
    IRestaurantTableRepository tableRepository,
    IDiningAreaRepository areaRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICreateRestaurantTableUseCase
{
    public async Task<Result<RestaurantTableDto>> ExecuteAsync(
        Guid tenantId,
        CreateRestaurantTableRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DiningAreaId == Guid.Empty
            || request.Capacity is < 1 or > 100
            || request.PositionX is < -100000 or > 100000
            || request.PositionY is < -100000 or > 100000
            || !TableRules.TryCleanName(request.Name, out var name, out var normalizedName))
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        var status = TableStatus.Available;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && (!TableRules.TryParseStatus(request.Status, out status) || status == TableStatus.Occupied))
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        var shape = TableShape.Square;
        if (!string.IsNullOrWhiteSpace(request.Shape)
            && !TableRules.TryParseShape(request.Shape, out shape))
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        if (await areaRepository.GetByIdAsync(tenantId, request.DiningAreaId, cancellationToken) is null)
            return Result<RestaurantTableDto>.Failure(Errors.DiningAreaNotFound);
        if (await tableRepository.NameExistsAsync(
            tenantId, request.DiningAreaId, normalizedName, null, cancellationToken))
            return Result<RestaurantTableDto>.Failure(Errors.RestaurantTableAlreadyExists);

        var now = clock.UtcNow;
        var table = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DiningAreaId = request.DiningAreaId,
            Name = name,
            NormalizedName = normalizedName,
            Capacity = request.Capacity,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            Shape = shape,
            IsDelivery = request.IsDelivery,
            IsCashRegister = request.IsCashRegister,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
        await tableRepository.AddAsync(table, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = TableRules.ToDto(table);
        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(dto), cancellationToken);
        return Result<RestaurantTableDto>.Success(dto);
    }
}

public sealed class UpdateRestaurantTableUseCase(
    IRestaurantTableRepository tableRepository,
    IDiningAreaRepository areaRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IUpdateRestaurantTableUseCase
{
    public async Task<Result<RestaurantTableDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        UpdateRestaurantTableRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DiningAreaId == Guid.Empty
            || request.Capacity is < 1 or > 100
            || request.PositionX is < -100000 or > 100000
            || request.PositionY is < -100000 or > 100000
            || !TableRules.TryCleanName(request.Name, out var name, out var normalizedName)
            || !TableRules.TryParseStatus(request.Status, out var status))
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        var table = await tableRepository.GetByIdAsync(tenantId, tableId, cancellationToken);
        if (table is null) return Result<RestaurantTableDto>.Failure(Errors.RestaurantTableNotFound);
        if (status == TableStatus.Occupied && table.Status != TableStatus.Occupied)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        var shape = table.Shape;
        if (!string.IsNullOrWhiteSpace(request.Shape)
            && !TableRules.TryParseShape(request.Shape, out shape))
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        if (await areaRepository.GetByIdAsync(tenantId, request.DiningAreaId, cancellationToken) is null)
            return Result<RestaurantTableDto>.Failure(Errors.DiningAreaNotFound);
        if (await tableRepository.NameExistsAsync(
            tenantId, request.DiningAreaId, normalizedName, tableId, cancellationToken))
            return Result<RestaurantTableDto>.Failure(Errors.RestaurantTableAlreadyExists);

        table.DiningAreaId = request.DiningAreaId;
        table.Name = name;
        table.NormalizedName = normalizedName;
        table.Capacity = request.Capacity;
        table.PositionX = request.PositionX;
        table.PositionY = request.PositionY;
        table.Shape = shape;
        table.IsDelivery = request.IsDelivery;
        table.IsCashRegister = request.IsCashRegister;
        table.Status = status;
        if (status != TableStatus.Occupied) ClearOrder(table);
        table.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = TableRules.ToDto(table);
        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(dto), cancellationToken);
        return Result<RestaurantTableDto>.Success(dto);
    }

    private static void ClearOrder(RestaurantTable table)
    {
        table.ActiveOrderId = null;
        table.ActiveOrderTotal = 0;
        table.OccupiedAt = null;
    }
}

public sealed class DeleteRestaurantTableUseCase(
    IRestaurantTableRepository repository,
    ITableRealtimeNotifier realtime,
    IUnitOfWork unitOfWork) : IDeleteRestaurantTableUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(tenantId, tableId, cancellationToken);
        if (table is null) return Result.Failure(Errors.RestaurantTableNotFound);
        if (table.Status == TableStatus.Occupied || table.ActiveOrderId.HasValue)
            return Result.Failure(Errors.RestaurantTableOccupied);
        var dto = TableRules.ToDto(table);
        repository.Remove(table);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(dto, true), cancellationToken);
        return Result.Success();
    }
}

public sealed class GetTableOperationUseCase(
    IRestaurantTableRepository tableRepository,
    ITenantRepository tenantRepository,
    ICashRegisterShiftRepository shiftRepository,
    IOrderRepository orderRepository,
    IExpenseRepository expenseRepository,
    IDateTimeProvider clock) : IGetTableOperationUseCase
{
    public async Task<Result<TableOperationSnapshotDto>> ExecuteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<TableOperationSnapshotDto>.Failure(Errors.TenantNotFound);
        var areas = await tableRepository.GetOperationAreasAsync(tenantId, cancellationToken);
        var openShift = await shiftRepository.GetOpenShiftAsync(tenantId, null, cancellationToken);
        var hasOpenShift = !tenant.RequiresOpenCashRegister || openShift is not null;

        var tableDtos = areas.SelectMany(area => area.Tables).Select(TableRules.ToDto).ToArray();
        var responseAreas = areas.Select(area => new DiningAreaTablesDto(
            TableRules.ToDto(area),
            area.Tables.OrderBy(table => table.NormalizedName).Select(TableRules.ToDto).ToArray())).ToArray();

        var utcNow = clock.UtcNow.ToUniversalTime();
        var todayStart = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, TimeSpan.Zero).AddDays(-1);
        var todayEnd = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 23, 59, 59, 999, TimeSpan.Zero).AddDays(1);

        var todayOrdersPage = await orderRepository.GetOrdersPageAsync(tenantId, new OrderQueryRequest
        {
            Page = 1,
            PageSize = 10000,
            Statuses = new[] { "PAID" },
            FromDate = todayStart,
            ToDate = todayEnd
        }, cancellationToken);
        var todaySalesTotal = todayOrdersPage.Items.Sum(o => o.TotalAmount);

        var todayExpensesPage = await expenseRepository.GetPageAsync(
            tenantId,
            fromDate: todayStart,
            toDate: todayEnd,
            search: null,
            supplierId: null,
            status: "ACTIVE",
            paymentMethod: null,
            isCashOut: null,
            page: 1,
            pageSize: 10000,
            cancellationToken);
        var todayExpensesTotal = todayExpensesPage.Items.Sum(e => e.Amount);

        decimal openShiftExpensesTotal = 0m;
        decimal openShiftSalesTotal = 0m;
        if (openShift is not null)
        {
            var shiftExpensesPage = await expenseRepository.GetPageAsync(
                tenantId,
                fromDate: openShift.OpenedAt,
                toDate: utcNow.AddDays(1),
                search: null,
                supplierId: null,
                status: "ACTIVE",
                paymentMethod: null,
                isCashOut: true,
                page: 1,
                pageSize: 10000,
                cancellationToken);
            openShiftExpensesTotal = shiftExpensesPage.Items.Sum(e => e.Amount);

            var shiftOrdersPage = await orderRepository.GetOrdersPageAsync(tenantId, new OrderQueryRequest
            {
                Page = 1,
                PageSize = 10000,
                Statuses = new[] { "PAID" },
                FromDate = openShift.OpenedAt,
                ToDate = utcNow.AddDays(1)
            }, cancellationToken);
            openShiftSalesTotal = shiftOrdersPage.Items.Sum(o => o.TotalAmount);
        }

        return Result<TableOperationSnapshotDto>.Success(new TableOperationSnapshotDto(
            responseAreas,
            new TableMetricsDto(
                tableDtos.Count(table => table.Status == "AVAILABLE"),
                tableDtos.Count(table => table.Status == "OCCUPIED"),
                tableDtos.Where(table => table.Status == "OCCUPIED").Sum(table => table.ActiveOrderTotal),
                todaySalesTotal,
                todayExpensesTotal,
                openShiftSalesTotal,
                openShiftExpensesTotal),
            new CashRegisterGateDto(
                tenant.RequiresOpenCashRegister,
                hasOpenShift,
                tenant.RequiresOpenCashRegister && !hasOpenShift)));
    }
}

public sealed class SetTableOperationUseCase(
    IRestaurantTableRepository tableRepository,
    ITenantRepository tenantRepository,
    ICashRegisterShiftRepository shiftRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ISetTableOperationUseCase
{
    public async Task<Result<RestaurantTableDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        SetTableOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TableRules.TryParseStatus(request.Status, out var status)
            || status == TableStatus.Disabled
            || request.ActiveOrderTotal is < 0 or > TableRules.MaximumTotal)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        var gate = await CheckGateAsync(tenantId, tenantRepository, shiftRepository, cancellationToken);
        if (!gate.IsSuccess) return Result<RestaurantTableDto>.Failure(gate.Error!);
        var table = await tableRepository.GetByIdAsync(tenantId, tableId, cancellationToken);
        if (table is null) return Result<RestaurantTableDto>.Failure(Errors.RestaurantTableNotFound);
        if (table.Status == TableStatus.Disabled)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);

        var now = clock.UtcNow;
        if (status == TableStatus.Occupied && !table.IsCashRegister)
        {
            table.Status = TableStatus.Occupied;
            table.ActiveOrderId = request.ActiveOrderId ?? table.ActiveOrderId ?? Guid.NewGuid();
            table.ActiveOrderTotal = request.ActiveOrderTotal;
            table.OccupiedAt ??= now;
        }
        else
        {
            table.Status = TableStatus.Available;
            table.ActiveOrderId = null;
            table.ActiveOrderTotal = 0;
            table.OccupiedAt = null;
        }
        table.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = TableRules.ToDto(table);
        await realtime.StatusChangedAsync(tenantId, new TableStatusChangedEvent(dto), cancellationToken);
        return Result<RestaurantTableDto>.Success(dto);
    }

    private static async Task<Result> CheckGateAsync(
        Guid tenantId,
        ITenantRepository tenantRepository,
        ICashRegisterShiftRepository shiftRepository,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(Errors.TenantNotFound);
        if (tenant.RequiresOpenCashRegister
            && !await shiftRepository.HasOpenShiftAsync(tenantId, cancellationToken))
            return Result.Failure(Errors.CashRegisterClosed);
        return Result.Success();
    }
}

public sealed class UpdateTableOrderUseCase(
    IRestaurantTableRepository tableRepository,
    ITenantRepository tenantRepository,
    ICashRegisterShiftRepository shiftRepository,
    ITableRealtimeNotifier realtime,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IUpdateTableOrderUseCase
{
    public async Task<Result<RestaurantTableDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        UpdateTableOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ActiveOrderId == Guid.Empty || request.Total is < 0 or > TableRules.MaximumTotal)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<RestaurantTableDto>.Failure(Errors.TenantNotFound);
        if (tenant.RequiresOpenCashRegister
            && !await shiftRepository.HasOpenShiftAsync(tenantId, cancellationToken))
            return Result<RestaurantTableDto>.Failure(Errors.CashRegisterClosed);
        var table = await tableRepository.GetByIdAsync(tenantId, tableId, cancellationToken);
        if (table is null) return Result<RestaurantTableDto>.Failure(Errors.RestaurantTableNotFound);
        if (table.Status != TableStatus.Occupied || table.ActiveOrderId != request.ActiveOrderId)
            return Result<RestaurantTableDto>.Failure(Errors.Validation);

        table.ActiveOrderTotal = request.Total;
        table.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await realtime.OrderUpdatedAsync(
            tenantId,
            new TableOrderUpdatedEvent(table.Id, request.ActiveOrderId, request.Total, table.UpdatedAt),
            cancellationToken);
        return Result<RestaurantTableDto>.Success(TableRules.ToDto(table));
    }
}
