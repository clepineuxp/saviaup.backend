using Moq;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Orders;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using Xunit;

namespace SaviaUp.Backend.Core.Tests;

public sealed class OrderUseCaseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _userName = "Test User";

    [Fact]
    public async Task AddTableOrderItems_CreatesOrderAndOccupiesTable()
    {
        var table = new RestaurantTable { Id = Guid.NewGuid(), TenantId = _tenantId, Status = TableStatus.Available };
        var tenant = new Tenant { Id = _tenantId, RequiresOpenCashRegister = false };

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetActiveByTableIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        orderRepo.Setup(r => r.GetNextOrderNumberAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var tableRepo = new Mock<IRestaurantTableRepository>();
        tableRepo.Setup(r => r.GetByIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var useCase = new AddTableOrderItemsUseCase(
            orderRepo.Object,
            tableRepo.Object,
            tenantRepo.Object,
            Mock.Of<ICashRegisterShiftRepository>(),
            Mock.Of<ITableRealtimeNotifier>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var request = new AddOrderItemsRequest([
            new CreateOrderItemRequest(Guid.NewGuid(), "Hamburguesa", 25000, 2, "Sin cebolla", false)
        ]);

        var result = await useCase.ExecuteAsync(_tenantId, table.Id, _userId, _userName, request, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(TableStatus.Occupied, table.Status);
        Assert.Equal(50000, result.Value!.TotalAmount);
        Assert.Single(result.Value.Items);
        Assert.Equal("PENDING", result.Value.Items.First().Status);
        Assert.Equal("Sin cebolla", result.Value.Items.First().Notes);
    }

    [Fact]
    public async Task CancelOrderItem_WithReason_UpdatesItemStatusAndRecalculatesTotal()
    {
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var table = new RestaurantTable { Id = Guid.NewGuid(), TenantId = _tenantId, Status = TableStatus.Occupied, ActiveOrderId = orderId };

        var item1 = new OrderItem { Id = itemId, OrderId = orderId, ProductName = "Jugo", UnitPrice = 8000, Quantity = 1, Subtotal = 8000, Status = "PENDING" };
        var item2 = new OrderItem { Id = Guid.NewGuid(), OrderId = orderId, ProductName = "Pizza", UnitPrice = 30000, Quantity = 1, Subtotal = 30000, Status = "PENDING" };

        var order = new Order
        {
            Id = orderId,
            TenantId = _tenantId,
            TableId = table.Id,
            Status = "PENDING",
            SubtotalAmount = 38000,
            TotalAmount = 38000,
            Items = new List<OrderItem> { item1, item2 }
        };

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetItemByIdAsync(_tenantId, itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item1);
        orderRepo.Setup(r => r.GetByIdAsync(_tenantId, orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var tableRepo = new Mock<IRestaurantTableRepository>();
        tableRepo.Setup(r => r.GetByIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = _tenantId, RequiresOpenCashRegister = false });

        var useCase = new CancelOrderItemUseCase(
            orderRepo.Object,
            tableRepo.Object,
            tenantRepo.Object,
            Mock.Of<ICashRegisterShiftRepository>(),
            Mock.Of<ITableRealtimeNotifier>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(_tenantId, itemId, _userId, _userName, new CancelOrderItemRequest("Cliente cambió de opinión"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("CANCELLED", item1.Status);
        Assert.Equal("Cliente cambió de opinión", item1.CancellationReason);
        Assert.Equal(30000, result.Value!.TotalAmount);
    }

    [Fact]
    public async Task PayAndCloseTableOrder_MarksItemsAsPaidAndFreesTable()
    {
        var orderId = Guid.NewGuid();
        var table = new RestaurantTable { Id = Guid.NewGuid(), TenantId = _tenantId, Status = TableStatus.Occupied, ActiveOrderId = orderId };

        var item = new OrderItem { Id = Guid.NewGuid(), OrderId = orderId, ProductName = "Menu", UnitPrice = 40000, Quantity = 1, Subtotal = 40000, Status = "PENDING" };
        var order = new Order
        {
            Id = orderId,
            TenantId = _tenantId,
            TableId = table.Id,
            Status = "PENDING",
            SubtotalAmount = 40000,
            TotalAmount = 40000,
            Items = new List<OrderItem> { item }
        };

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetActiveByTableIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var tableRepo = new Mock<IRestaurantTableRepository>();
        tableRepo.Setup(r => r.GetByIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(table);

        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = _tenantId, RequiresOpenCashRegister = false });

        var useCase = new PayAndCloseTableOrderUseCase(
            orderRepo.Object,
            tableRepo.Object,
            tenantRepo.Object,
            Mock.Of<ICashRegisterShiftRepository>(),
            Mock.Of<ITableRealtimeNotifier>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(_tenantId, table.Id, _userId, _userName, new CheckoutOrderRequest("Tarjeta/Datafono", TipAmount: 4000), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("PAID", result.Value!.Status);
        Assert.Equal(44000, result.Value.TotalAmount);
        Assert.Equal(TableStatus.Available, table.Status);
        Assert.Null(table.ActiveOrderId);
    }
}
