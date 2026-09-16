using Moq;
using SaviaUp.Backend.Core.Inventory;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class InventoryUseCaseTests
{
    [Fact]
    public async Task CreateIngredient_WithInitialStock_PersistsInitialMovement()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var category = Category(tenantId, inventoryTracked: true);
        var unit = Unit(tenantId);
        var ingredients = new Mock<IIngredientRepository>();
        var movements = new Mock<IInventoryMovementRepository>();
        var categories = new Mock<ICategoryRepository>();
        var units = new Mock<IMeasurementUnitRepository>();
        Ingredient? persistedIngredient = null;
        InventoryMovement? persistedMovement = null;
        categories.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        units.Setup(value => value.GetByIdAsync(tenantId, unit.Id, It.IsAny<CancellationToken>())).ReturnsAsync(unit);
        ingredients.Setup(value => value.AddAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()))
            .Callback<Ingredient, CancellationToken>((value, _) => persistedIngredient = value).Returns(Task.CompletedTask);
        movements.Setup(value => value.AddAsync(It.IsAny<InventoryMovement>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryMovement, CancellationToken>((value, _) => persistedMovement = value).Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        var useCase = new CreateIngredientUseCase(
            ingredients.Object, movements.Object, categories.Object, units.Object,
            new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            tenantId, userId,
            new CreateIngredientRequest(category.Id, unit.Id, "  Café   molido ", "  Tueste medio  ", 2.5m, 10m),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedIngredient);
        Assert.Equal(("Café molido", "CAFÉ MOLIDO", 2.5m, 10m),
            (persistedIngredient.Name, persistedIngredient.NormalizedName, persistedIngredient.MinimumStock, persistedIngredient.CurrentStock));
        Assert.NotNull(persistedMovement);
        Assert.Equal((InventoryMovementCodes.Increase, InventoryMovementCodes.Initial, 0m, 10m, userId),
            (persistedMovement.Direction, persistedMovement.Reason, persistedMovement.StockBefore, persistedMovement.StockAfter, persistedMovement.CreatedByUserId));
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIngredient_WithCategoryFromAnotherTenant_ReturnsNotFound()
    {
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(value => value.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        var ingredients = new Mock<IIngredientRepository>();
        var useCase = new CreateIngredientUseCase(
            ingredients.Object, Mock.Of<IInventoryMovementRepository>(), categories.Object,
            Mock.Of<IMeasurementUnitRepository>(), new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new CreateIngredientRequest(Guid.NewGuid(), Guid.NewGuid(), "Harina", null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CategoryNotFound, result.Error!.Code);
        ingredients.Verify(value => value.AddAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("increase", "purchase", 15)]
    [InlineData("decrease", "waste", 5)]
    public async Task CreateMovement_WithValidDirection_UpdatesStockAndKeepsAudit(
        string direction, string reason, decimal expectedStock)
    {
        var tenantId = Guid.NewGuid();
        var ingredient = Ingredient(tenantId, currentStock: 10m);
        var ingredients = new Mock<IIngredientRepository>();
        var movements = new Mock<IInventoryMovementRepository>();
        InventoryMovement? persisted = null;
        ingredients.Setup(value => value.GetForStockUpdateAsync(tenantId, ingredient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingredient);
        movements.Setup(value => value.AddAsync(It.IsAny<InventoryMovement>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryMovement, CancellationToken>((value, _) => persisted = value).Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        TestSupport.RunTransaction<Result<InventoryMovementDto>>(unitOfWork);
        var useCase = new CreateInventoryMovementUseCase(
            ingredients.Object, movements.Object, new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            tenantId, Guid.NewGuid(), new CreateInventoryMovementRequest(ingredient.Id, direction, reason, 5m, " ajuste "), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedStock, ingredient.CurrentStock);
        Assert.NotNull(persisted);
        Assert.Equal((10m, expectedStock, "ajuste"), (persisted.StockBefore, persisted.StockAfter, persisted.Note));
    }

    [Fact]
    public async Task CreateMovement_WhenDecreaseExceedsStock_ReturnsBusinessError()
    {
        var tenantId = Guid.NewGuid();
        var ingredient = Ingredient(tenantId, currentStock: 2m);
        var ingredients = new Mock<IIngredientRepository>();
        ingredients.Setup(value => value.GetForStockUpdateAsync(tenantId, ingredient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingredient);
        var movements = new Mock<IInventoryMovementRepository>();
        var unitOfWork = UnitOfWork();
        TestSupport.RunTransaction<Result<InventoryMovementDto>>(unitOfWork);
        var useCase = new CreateInventoryMovementUseCase(
            ingredients.Object, movements.Object, new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            tenantId, Guid.NewGuid(), new CreateInventoryMovementRequest(
                ingredient.Id, InventoryMovementCodes.Decrease, InventoryMovementCodes.Expiration, 3m, null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.InventoryInsufficientStock, result.Error!.Code);
        Assert.Equal(2m, ingredient.CurrentStock);
        movements.Verify(value => value.AddAsync(It.IsAny<InventoryMovement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateMovement_WithReasonForOppositeDirection_ReturnsValidationError()
    {
        var unitOfWork = UnitOfWork();
        TestSupport.RunTransaction<Result<InventoryMovementDto>>(unitOfWork);
        var ingredients = new Mock<IIngredientRepository>();
        var useCase = new CreateInventoryMovementUseCase(
            ingredients.Object, Mock.Of<IInventoryMovementRepository>(), new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new CreateInventoryMovementRequest(
                Guid.NewGuid(), InventoryMovementCodes.Increase, InventoryMovementCodes.Loss, 1m, null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.InventoryMovementInvalid, result.Error!.Code);
        ingredients.Verify(value => value.GetForStockUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateMeasurementUnit_NormalizesCodeAndName()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IMeasurementUnitRepository>();
        MeasurementUnit? persisted = null;
        repository.Setup(value => value.CodeOrNameExistsAsync(
            tenantId, "ML", "MILILITROS", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(value => value.AddAsync(It.IsAny<MeasurementUnit>(), It.IsAny<CancellationToken>()))
            .Callback<MeasurementUnit, CancellationToken>((value, _) => persisted = value).Returns(Task.CompletedTask);
        var useCase = new CreateMeasurementUnitUseCase(
            repository.Object, new FixedClock(TestSupport.Now), UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            tenantId, new CreateMeasurementUnitRequest(" ML ", "  mililitros "), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(("ml", "ML", "mililitros", "MILILITROS"),
            (persisted.Code, persisted.NormalizedCode, persisted.Name, persisted.NormalizedName));
    }

    [Fact]
    public async Task DeleteIngredient_WithMovements_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var ingredient = Ingredient(tenantId);
        var repository = new Mock<IIngredientRepository>();
        repository.Setup(value => value.GetByIdAsync(tenantId, ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ingredient);
        repository.Setup(value => value.HasMovementsAsync(tenantId, ingredient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var useCase = new DeleteIngredientUseCase(repository.Object, UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(tenantId, ingredient.Id, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.IngredientInUse, result.Error!.Code);
        repository.Verify(value => value.Remove(It.IsAny<Ingredient>()), Times.Never);
    }

    [Fact]
    public async Task ListInventory_ReturnsPaginationMetadata()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IIngredientRepository>();
        var item = new InventoryItemDto(Guid.NewGuid(), "ingredient", "Café", new(Guid.NewGuid(), "Bebidas", true),
            new(Guid.NewGuid(), "gr", "gramos", true, TestSupport.Now, TestSupport.Now), 1m, 5m, true);
        repository.Setup(value => value.GetInventoryPageAsync(tenantId, It.IsAny<InventoryQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageData<InventoryItemDto>([item], 21));
        var useCase = new ListInventoryUseCase(repository.Object);

        var result = await useCase.ExecuteAsync(tenantId, new InventoryQueryRequest { Page = 2, PageSize = 10 }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal((2, 10, 21, 3), (result.Value!.Page, result.Value.PageSize, result.Value.TotalCount, result.Value.TotalPages));
        Assert.Single(result.Value.Items);
    }

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private static Category Category(Guid tenantId, bool inventoryTracked = false) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Materia prima",
        NormalizedName = "MATERIA PRIMA",
        IsInventoryTracked = inventoryTracked,
        IsActive = true,
        CreatedAt = TestSupport.Now,
        UpdatedAt = TestSupport.Now
    };

    private static MeasurementUnit Unit(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Code = "gr",
        NormalizedCode = "GR",
        Name = "gramos",
        NormalizedName = "GRAMOS",
        IsActive = true,
        CreatedAt = TestSupport.Now,
        UpdatedAt = TestSupport.Now
    };

    private static Ingredient Ingredient(Guid tenantId, decimal currentStock = 0)
    {
        var category = Category(tenantId, inventoryTracked: true);
        var unit = Unit(tenantId);
        return new Ingredient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category.Id,
            Category = category,
            MeasurementUnitId = unit.Id,
            MeasurementUnit = unit,
            Name = "Harina",
            NormalizedName = "HARINA",
            CurrentStock = currentStock,
            IsActive = true,
            CreatedAt = TestSupport.Now,
            UpdatedAt = TestSupport.Now
        };
    }
}
