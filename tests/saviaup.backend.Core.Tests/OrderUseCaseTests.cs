using Moq;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Orders;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;
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

        var printJobFactory = new Mock<IPrintJobFactory>();
        printJobFactory.Setup(x => x.CreateForOrderItemsAsync(
                It.IsAny<Guid>(), It.IsAny<Order>(), It.IsAny<IReadOnlyCollection<OrderItem>>(),
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PrintJobNotification>());
        var useCase = new AddTableOrderItemsUseCase(
            orderRepo.Object,
            tableRepo.Object,
            tenantRepo.Object,
            Mock.Of<ICashRegisterShiftRepository>(),
            Mock.Of<ITableRealtimeNotifier>(),
            printJobFactory.Object,
            Mock.Of<IPrintingRealtimeNotifier>(),
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
    public async Task AddTableOrderItems_ConfiguredCombo_ValidatesSelectionsAndCalculatesPrice()
    {
        var table = new RestaurantTable { Id = Guid.NewGuid(), TenantId = _tenantId, Status = TableStatus.Available };
        var includedProduct = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, Type = ProductType.Normal, Name = "Arepa", IsActive = true };
        var fixedProduct = new Product { Id = Guid.NewGuid(), TenantId = _tenantId, Type = ProductType.Normal, Name = "Café", IsActive = true };
        var combo = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Type = ProductType.Combo,
            Name = "Combo desayuno",
            SalePrice = 20000m,
            IsActive = true
        };
        var group = new ProductComboGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ComboProductId = combo.Id,
            Name = "Acompañantes",
            SelectionType = ProductComboSelectionType.Multiple,
            IsRequired = true,
            MinSelections = 1,
            MaxSelections = 2
        };
        var option = new ProductComboOption
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ComboGroupId = group.Id,
            ProductId = includedProduct.Id,
            Product = includedProduct,
            ProductQuantity = 1,
            PriceAdjustment = 1000m
        };
        group.Options.Add(option);
        combo.ComboGroups.Add(group);
        var fixedGroup = new ProductComboGroup
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ComboProductId = combo.Id,
            Name = "Incluidos",
            SelectionType = ProductComboSelectionType.Fixed,
            IsRequired = true,
            MinSelections = 1,
            MaxSelections = 1
        };
        fixedGroup.Options.Add(new ProductComboOption
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ComboGroupId = fixedGroup.Id,
            ProductId = fixedProduct.Id,
            Product = fixedProduct,
            ProductQuantity = 2,
            PriceAdjustment = -500m
        });
        combo.ComboGroups.Add(fixedGroup);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetActiveByTableIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);
        orderRepo.Setup(r => r.GetNextOrderNumberAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var tableRepo = new Mock<IRestaurantTableRepository>();
        tableRepo.Setup(r => r.GetByIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>())).ReturnsAsync(table);
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = _tenantId, RequiresOpenCashRegister = false });
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsWithRecipesAsync(_tenantId, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([combo]);
        var printJobs = new Mock<IPrintJobFactory>();
        printJobs.Setup(x => x.CreateForOrderItemsAsync(
                It.IsAny<Guid>(), It.IsAny<Order>(), It.IsAny<IReadOnlyCollection<OrderItem>>(),
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var useCase = new AddTableOrderItemsUseCase(
            orderRepo.Object, tableRepo.Object, tenantRepo.Object, Mock.Of<ICashRegisterShiftRepository>(),
            Mock.Of<ITableRealtimeNotifier>(), printJobs.Object, Mock.Of<IPrintingRealtimeNotifier>(),
            new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>(), productRepo.Object);

        var result = await useCase.ExecuteAsync(
            _tenantId, table.Id, _userId, _userName,
            new AddOrderItemsRequest([
                new CreateOrderItemRequest(
                    combo.Id, combo.Name, combo.SalePrice, 1, "Sin azúcar", false,
                    [new CreateOrderItemComboSelectionRequest(group.Id, option.Id, 2)])
            ]),
            default);

        Assert.True(result.IsSuccess);
        var orderItem = Assert.Single(result.Value!.Items);
        Assert.Equal(21500m, orderItem.UnitPrice);
        Assert.Equal(2, orderItem.ComboSelections.Count);
        Assert.Equal(2, orderItem.ComboSelections.Single(selection => selection.ComboGroupId == group.Id).SelectionQuantity);
        Assert.Equal(1, orderItem.ComboSelections.Single(selection => selection.ComboGroupId == fixedGroup.Id).SelectionQuantity);
        Assert.Equal(
            "Combo: Acompañantes: 2× Arepa; Incluidos: 2× Café | Observaciones adicionales: Sin azúcar",
            orderItem.Notes);
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
            Mock.Of<IProductRepository>(),
            Mock.Of<IIngredientRepository>(),
            Mock.Of<IInventoryMovementRepository>(),
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

    [Fact]
    public async Task PayAndCloseTableOrder_WithProductRecipe_DeductsInventoryAndCreatesMovements()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var table = new RestaurantTable { Id = Guid.NewGuid(), TenantId = _tenantId, Status = TableStatus.Occupied, ActiveOrderId = orderId, Name = "Mesa 1" };

        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = "Hamburguesa Especial",
            UnitPrice = 30000,
            Quantity = 2,
            Subtotal = 60000,
            Status = "PENDING"
        };
        var order = new Order
        {
            Id = orderId,
            TenantId = _tenantId,
            TableId = table.Id,
            OrderNumber = 101,
            Status = "PENDING",
            SubtotalAmount = 60000,
            TotalAmount = 60000,
            Items = new List<OrderItem> { item }
        };

        var ingredient = new Ingredient
        {
            Id = ingredientId,
            TenantId = _tenantId,
            Name = "Carne 150g",
            CurrentStock = 10m
        };

        var product = new Product
        {
            Id = productId,
            TenantId = _tenantId,
            Name = "Hamburguesa Especial",
            RecipeItems = new List<ProductRecipeItem>
            {
                new ProductRecipeItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantId,
                    ProductId = productId,
                    IngredientId = ingredientId,
                    Quantity = 1.5m
                }
            }
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

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsWithRecipesAsync(_tenantId, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });

        var ingredientRepo = new Mock<IIngredientRepository>();
        ingredientRepo.Setup(r => r.GetForStockUpdateAsync(_tenantId, ingredientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingredient);

        var movementRepo = new Mock<IInventoryMovementRepository>();
        InventoryMovement? createdMovement = null;
        movementRepo.Setup(r => r.AddAsync(It.IsAny<InventoryMovement>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryMovement, CancellationToken>((m, _) => createdMovement = m)
            .Returns(Task.CompletedTask);

        var useCase = new PayAndCloseTableOrderUseCase(
            orderRepo.Object,
            tableRepo.Object,
            tenantRepo.Object,
            Mock.Of<ICashRegisterShiftRepository>(),
            productRepo.Object,
            ingredientRepo.Object,
            movementRepo.Object,
            Mock.Of<ITableRealtimeNotifier>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(_tenantId, table.Id, _userId, _userName, new CheckoutOrderRequest("Efectivo"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("PAID", result.Value!.Status);
        // Initial stock 10 - (1.5 * 2 = 3) = 7
        Assert.Equal(7m, ingredient.CurrentStock);
        Assert.NotNull(createdMovement);
        Assert.Equal(3m, createdMovement.Quantity);
        Assert.Equal(10m, createdMovement.StockBefore);
        Assert.Equal(7m, createdMovement.StockAfter);
        Assert.Equal(InventoryMovementCodes.Decrease, createdMovement.Direction);
        Assert.Equal(InventoryMovementCodes.Sale, createdMovement.Reason);
    }

    [Fact]
    public async Task PayAndCloseTableOrder_WithComboSelections_DeductsSelectedProductsRecipes()
    {
        var orderId = Guid.NewGuid();
        var componentProductId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var table = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Status = TableStatus.Occupied,
            ActiveOrderId = orderId,
            Name = "Mesa combo"
        };
        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = Guid.NewGuid(),
            ProductName = "Combo",
            UnitPrice = 40000m,
            Quantity = 2,
            Subtotal = 80000m,
            Status = "PENDING"
        };
        item.ComboSelections.Add(new OrderItemComboSelection
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrderItemId = item.Id,
            ProductId = componentProductId,
            GroupName = "Bebidas",
            ProductName = "Jugo",
            ProductQuantity = 2,
            SelectionQuantity = 1
        });
        var order = new Order
        {
            Id = orderId,
            TenantId = _tenantId,
            TableId = table.Id,
            OrderNumber = 102,
            Status = "PENDING",
            SubtotalAmount = 80000m,
            TotalAmount = 80000m,
            Items = [item]
        };
        var ingredient = new Ingredient { Id = ingredientId, TenantId = _tenantId, Name = "Pulpa", CurrentStock = 20m };
        var component = new Product
        {
            Id = componentProductId,
            TenantId = _tenantId,
            Name = "Jugo",
            RecipeItems = [new ProductRecipeItem { Id = Guid.NewGuid(), TenantId = _tenantId, ProductId = componentProductId, IngredientId = ingredientId, Quantity = 0.5m }]
        };
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetActiveByTableIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var tableRepo = new Mock<IRestaurantTableRepository>();
        tableRepo.Setup(r => r.GetByIdAsync(_tenantId, table.Id, It.IsAny<CancellationToken>())).ReturnsAsync(table);
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = _tenantId, RequiresOpenCashRegister = false });
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdsWithRecipesAsync(_tenantId, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([component]);
        var ingredientRepo = new Mock<IIngredientRepository>();
        ingredientRepo.Setup(r => r.GetForStockUpdateAsync(_tenantId, ingredientId, It.IsAny<CancellationToken>())).ReturnsAsync(ingredient);
        var useCase = new PayAndCloseTableOrderUseCase(
            orderRepo.Object, tableRepo.Object, tenantRepo.Object, Mock.Of<ICashRegisterShiftRepository>(),
            productRepo.Object, ingredientRepo.Object, Mock.Of<IInventoryMovementRepository>(), Mock.Of<ITableRealtimeNotifier>(),
            new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            _tenantId, table.Id, _userId, _userName, new CheckoutOrderRequest("Efectivo"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(18m, ingredient.CurrentStock); // 0.5 receta × 2 unidades incluidas × 2 combos
    }
}
