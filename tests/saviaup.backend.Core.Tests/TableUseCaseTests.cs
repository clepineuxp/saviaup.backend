using Moq;
using SaviaUp.Backend.Core.Tables;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class TableUseCaseTests
{
    [Fact]
    public async Task CreateArea_NormalizesNameAndRejectsTenantDuplicate()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IDiningAreaRepository>();
        repository.Setup(value => value.GetNextOrderAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        repository.Setup(value => value.NameExistsAsync(
                tenantId, "SALÓN PRINCIPAL", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var useCase = new CreateDiningAreaUseCase(
            repository.Object,
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            tenantId,
            new CreateDiningAreaRequest("  Salón   principal "),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.DiningAreaAlreadyExists, result.Error!.Code);
        repository.Verify(value => value.AddAsync(It.IsAny<DiningArea>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetOperation_WhenCashRegisterIsRequiredAndClosed_BlocksMutation()
    {
        var tenantId = Guid.NewGuid();
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, RequiresOpenCashRegister = true });
        var shifts = new Mock<ICashRegisterShiftRepository>();
        shifts.Setup(value => value.HasOpenShiftAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var tables = new Mock<IRestaurantTableRepository>();
        var useCase = new SetTableOperationUseCase(
            tables.Object,
            tenants.Object,
            shifts.Object,
            Mock.Of<ITableRealtimeNotifier>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            tenantId,
            Guid.NewGuid(),
            new SetTableOperationRequest("OCCUPIED"),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CashRegisterClosed, result.Error!.Code);
        tables.Verify(value => value.GetByIdAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTable_PersistsAndReturnsRequestedShape()
    {
        var tenantId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        RestaurantTable? persisted = null;
        var areas = new Mock<IDiningAreaRepository>();
        areas.Setup(value => value.GetByIdAsync(tenantId, areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiningArea { Id = areaId, TenantId = tenantId });
        var tables = new Mock<IRestaurantTableRepository>();
        tables.Setup(value => value.AddAsync(It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>()))
            .Callback<RestaurantTable, CancellationToken>((table, _) => persisted = table)
            .Returns(Task.CompletedTask);
        var realtime = new Mock<ITableRealtimeNotifier>();
        realtime.Setup(value => value.StatusChangedAsync(
                tenantId, It.IsAny<TableStatusChangedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var useCase = new CreateRestaurantTableUseCase(
            tables.Object,
            areas.Object,
            realtime.Object,
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            tenantId,
            new CreateRestaurantTableRequest(
                areaId,
                "Mesa rectangular",
                6,
                20,
                30,
                false,
                false,
                Shape: "RECTANGLE_HORIZONTAL"),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(TableShape.RectangleHorizontal, persisted.Shape);
        Assert.Equal("RECTANGLE_HORIZONTAL", result.Value!.Shape);
    }

    [Fact]
    public async Task CreateTable_WithUnknownShape_ReturnsValidationError()
    {
        var tables = new Mock<IRestaurantTableRepository>();
        var useCase = new CreateRestaurantTableUseCase(
            tables.Object,
            Mock.Of<IDiningAreaRepository>(),
            Mock.Of<ITableRealtimeNotifier>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new CreateRestaurantTableRequest(
                Guid.NewGuid(),
                "Mesa inválida",
                4,
                0,
                0,
                false,
                false,
                Shape: "TRIANGLE"),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Error!.Code);
        tables.Verify(value => value.AddAsync(
            It.IsAny<RestaurantTable>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetOperation_OccupiesTableAndBroadcastsTenantEvent()
    {
        var tenantId = Guid.NewGuid();
        var table = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DiningAreaId = Guid.NewGuid(),
            Name = "Mesa 01",
            NormalizedName = "MESA 01",
            Capacity = 4,
            Status = TableStatus.Available,
            CreatedAt = TestSupport.Now,
            UpdatedAt = TestSupport.Now
        };
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId });
        var tables = new Mock<IRestaurantTableRepository>();
        tables.Setup(value => value.GetByIdAsync(tenantId, table.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);
        var realtime = new Mock<ITableRealtimeNotifier>();
        realtime.Setup(value => value.StatusChangedAsync(
                tenantId, It.IsAny<TableStatusChangedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var useCase = new SetTableOperationUseCase(
            tables.Object,
            tenants.Object,
            Mock.Of<ICashRegisterShiftRepository>(),
            realtime.Object,
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            tenantId,
            table.Id,
            new SetTableOperationRequest("OCCUPIED", ActiveOrderTotal: 18500m),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(TableStatus.Occupied, table.Status);
        Assert.NotNull(table.ActiveOrderId);
        Assert.Equal(18500m, table.ActiveOrderTotal);
        Assert.Equal(TestSupport.Now, table.OccupiedAt);
        realtime.Verify(value => value.StatusChangedAsync(
            tenantId,
            It.Is<TableStatusChangedEvent>(notification => notification.Table.Status == "OCCUPIED"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OperationSnapshot_ComputesMetricsAcrossActiveAreas()
    {
        var tenantId = Guid.NewGuid();
        var area = new DiningArea
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Terraza",
            NormalizedName = "TERRAZA",
            Order = 1,
            IsActive = true,
            CreatedAt = TestSupport.Now,
            UpdatedAt = TestSupport.Now
        };
        area.Tables.Add(Table(area.Id, tenantId, TableStatus.Available, 0));
        area.Tables.Add(Table(area.Id, tenantId, TableStatus.Occupied, 42000));
        var tables = new Mock<IRestaurantTableRepository>();
        tables.Setup(value => value.GetOperationAreasAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([area]);
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId });
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetOrdersPageAsync(tenantId, It.IsAny<OrderQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageData<OrderDto>([], 0));

        var expenseRepo = new Mock<IExpenseRepository>();
        expenseRepo.Setup(r => r.GetPageAsync(tenantId, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageData<Expense>([], 0));

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestSupport.Now);

        var useCase = new GetTableOperationUseCase(
            tables.Object, tenants.Object, Mock.Of<ICashRegisterShiftRepository>(), orderRepo.Object, expenseRepo.Object, clock.Object);

        var result = await useCase.ExecuteAsync(tenantId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Metrics.Available);
        Assert.Equal(1, result.Value.Metrics.Occupied);
        Assert.Equal(42000m, result.Value.Metrics.ActiveSalesTotal);
        Assert.False(result.Value.CashRegister.IsInteractionBlocked);
    }

    private static RestaurantTable Table(Guid areaId, Guid tenantId, TableStatus status, decimal total)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DiningAreaId = areaId,
            Name = Guid.NewGuid().ToString(),
            NormalizedName = Guid.NewGuid().ToString(),
            Capacity = 2,
            Status = status,
            ActiveOrderId = status == TableStatus.Occupied ? Guid.NewGuid() : null,
            ActiveOrderTotal = total,
            OccupiedAt = status == TableStatus.Occupied ? TestSupport.Now : null,
            CreatedAt = TestSupport.Now,
            UpdatedAt = TestSupport.Now
        };
}
