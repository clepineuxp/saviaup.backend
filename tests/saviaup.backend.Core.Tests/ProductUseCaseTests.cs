using Moq;
using SaviaUp.Backend.Core.Products;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class ProductUseCaseTests
{
    [Fact]
    public async Task CreateProduct_DefaultsToNormalAndForcesInventoryOffForNonInventoryCategory()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId, inventoryTracked: false);
        var categories = new Mock<ICategoryRepository>();
        var products = new Mock<IProductRepository>();
        Product? persisted = null;
        categories.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        products.Setup(value => value.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((value, _) => persisted = value)
            .Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        var useCase = new CreateProductUseCase(
            products.Object, categories.Object, new FixedClock(TestSupport.Now), unitOfWork.Object);

        var userId = Guid.NewGuid();
        var result = await useCase.ExecuteAsync(
            tenantId,
            userId,
            "admin@saviaup.test",
            new CreateProductRequest(
                string.Empty, "  Hamburguesa   clásica ", category.Id, 25900m,
                " Preparada al momento ", " https://cdn.saviaup.test/products/burger.webp ", 15, true),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(ProductType.Normal, persisted.Type);
        Assert.Equal(("Hamburguesa clásica", "HAMBURGUESA CLÁSICA"), (persisted.Name, persisted.NormalizedName));
        Assert.False(persisted.IsInventoryTracked);
        Assert.Equal("NORMAL", result.Value!.Type);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProduct_AllowsInventoryOnlyForInventoryCategory()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId, inventoryTracked: true);
        var categories = new Mock<ICategoryRepository>();
        var products = new Mock<IProductRepository>();
        Product? persisted = null;
        categories.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        products.Setup(value => value.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((value, _) => persisted = value)
            .Returns(Task.CompletedTask);
        var useCase = new CreateProductUseCase(
            products.Object, categories.Object, new FixedClock(TestSupport.Now), UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            Guid.NewGuid(),
            "admin@saviaup.test",
            new CreateProductRequest("NORMAL", "Combo familiar", category.Id, 50000m, null, null, null, true),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductType.Normal, persisted!.Type);
        Assert.True(persisted.IsInventoryTracked);
    }

    [Fact]
    public async Task CreateCombo_WithConfiguredGroup_PersistsNormalProductOptionsAndDisablesDirectInventory()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        var includedProduct = Product(tenantId);
        var categories = new Mock<ICategoryRepository>();
        var products = new Mock<IProductRepository>();
        Product? persisted = null;
        categories.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        products.Setup(value => value.GetByIdsWithRecipesAsync(
                tenantId,
                It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { includedProduct.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([includedProduct]);
        products.Setup(value => value.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((value, _) => persisted = value)
            .Returns(Task.CompletedTask);
        var useCase = new CreateProductUseCase(
            products.Object, categories.Object, new FixedClock(TestSupport.Now), UnitOfWork().Object);

        var groups = new[]
        {
            new ProductComboGroupRequest(
                "Elige acompañante", "MULTIPLE", true, 1, 2,
                [new ProductComboOptionRequest(includedProduct.Id, 2, 1500m)])
        };
        var result = await useCase.ExecuteAsync(
            tenantId,
            Guid.NewGuid(),
            "admin@saviaup.test",
            new CreateProductRequest(
                "COMBO", "Combo familiar", category.Id, 50000m, null, null, null, true,
                ComboGroups: groups),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(ProductType.Combo, persisted.Type);
        Assert.False(persisted.IsInventoryTracked);
        var group = Assert.Single(persisted.ComboGroups);
        var option = Assert.Single(group.Options);
        Assert.Equal((includedProduct.Id, 2, 1500m), (option.ProductId, option.ProductQuantity, option.PriceAdjustment));
        Assert.Single(result.Value!.ComboGroups);
    }

    [Fact]
    public async Task CreateCombo_WithoutGroups_ReturnsValidation()
    {
        var useCase = new CreateProductUseCase(
            Mock.Of<IProductRepository>(),
            Mock.Of<ICategoryRepository>(),
            new FixedClock(TestSupport.Now),
            UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), "admin@saviaup.test",
            new CreateProductRequest("COMBO", "Combo", Guid.NewGuid(), 100m, null, null, null, false),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Error!.Code);
    }

    [Fact]
    public async Task CreateCombo_WithOptionalFixedGroup_ReturnsValidation()
    {
        var useCase = new CreateProductUseCase(
            Mock.Of<IProductRepository>(),
            Mock.Of<ICategoryRepository>(),
            new FixedClock(TestSupport.Now),
            UnitOfWork().Object);
        var groups = new[]
        {
            new ProductComboGroupRequest(
                "Incluidos", "FIXED", false, 0, 1,
                [new ProductComboOptionRequest(Guid.NewGuid(), 1)])
        };

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), "admin@saviaup.test",
            new CreateProductRequest(
                "COMBO", "Combo", Guid.NewGuid(), 100m, null, null, null, false,
                ComboGroups: groups),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Error!.Code);
    }

    [Theory]
    [InlineData("INVALID", 100)]
    [InlineData("NORMAL", 0)]
    [InlineData("NORMAL", -1)]
    public async Task CreateProduct_WithInvalidTypeOrPrice_ReturnsValidation(string type, decimal price)
    {
        var products = new Mock<IProductRepository>();
        var categories = new Mock<ICategoryRepository>();
        var useCase = new CreateProductUseCase(
            products.Object, categories.Object, new FixedClock(TestSupport.Now), UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "admin@saviaup.test",
            new CreateProductRequest(type, "Producto", Guid.NewGuid(), price, null, null, null, false),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Error!.Code);
        categories.Verify(value => value.GetByIdAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProduct_WithCategoryOutsideTenant_ReturnsCategoryNotFound()
    {
        var tenantId = Guid.NewGuid();
        var product = Product(tenantId);
        var products = new Mock<IProductRepository>();
        var categories = new Mock<ICategoryRepository>();
        products.Setup(value => value.GetByIdForUpdateAsync(tenantId, product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        categories.Setup(value => value.GetByIdAsNoTrackingAsync(tenantId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        var useCase = new UpdateProductUseCase(
            products.Object, categories.Object, new FixedClock(TestSupport.Now), UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            product.Id,
            Guid.NewGuid(),
            "admin@saviaup.test",
            new UpdateProductRequest("NORMAL", "Producto", Guid.NewGuid(), 100m, null, null, null, false),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CategoryNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ListProducts_ParsesTypeAndReturnsPagination()
    {
        var tenantId = Guid.NewGuid();
        var products = new Mock<IProductRepository>();
        products.Setup(value => value.GetPageAsync(
                tenantId,
                It.IsAny<ProductQueryRequest>(),
                ProductType.Combo,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageData<Product>([Product(tenantId, ProductType.Combo)], 21));
        var useCase = new ListProductsUseCase(products.Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            new ProductQueryRequest { Page = 2, PageSize = 10, Type = "combo" },
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal((21, 3, "COMBO"),
            (result.Value!.TotalCount, result.Value.TotalPages, Assert.Single(result.Value.Items).Type));
    }

    [Fact]
    public async Task SetProductStatus_UpdatesExistingTenantProduct()
    {
        var tenantId = Guid.NewGuid();
        var product = Product(tenantId);
        var products = new Mock<IProductRepository>();
        products.Setup(value => value.GetByIdAsync(tenantId, product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        var useCase = new SetProductStatusUseCase(
            products.Object, new FixedClock(TestSupport.Now), UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            tenantId, product.Id, new SetProductStatusRequest(false), default);

        Assert.True(result.IsSuccess);
        Assert.False(product.IsActive);
        Assert.Equal(TestSupport.Now, product.UpdatedAt);
    }

    [Fact]
    public async Task SetProductStatus_WhenProductIsUsedByCombo_ReturnsProductInUse()
    {
        var tenantId = Guid.NewGuid();
        var product = Product(tenantId);
        var products = new Mock<IProductRepository>();
        products.Setup(value => value.GetByIdAsync(tenantId, product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        products.Setup(value => value.IsUsedInComboAsync(tenantId, product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var unitOfWork = UnitOfWork();
        var useCase = new SetProductStatusUseCase(
            products.Object, new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            tenantId, product.Id, new SetProductStatusRequest(false), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ProductInUse, result.Error!.Code);
        Assert.True(product.IsActive);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProduct_OutsideTenant_ReturnsNotFound()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(value => value.GetByIdAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        var unitOfWork = UnitOfWork();
        var useCase = new DeleteProductUseCase(products.Object, unitOfWork.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ProductNotFound, result.Error!.Code);
        products.Verify(value => value.Remove(It.IsAny<Product>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private static Category Category(Guid tenantId, bool inventoryTracked = true) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Platos",
        NormalizedName = "PLATOS",
        IsInventoryTracked = inventoryTracked,
        IsActive = true,
        CreatedAt = TestSupport.Now,
        UpdatedAt = TestSupport.Now
    };

    private static Product Product(Guid tenantId, ProductType type = ProductType.Normal)
    {
        var category = Category(tenantId);
        return new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category.Id,
            Category = category,
            Type = type,
            Name = "Producto",
            NormalizedName = "PRODUCTO",
            SalePrice = 100m,
            IsActive = true,
            CreatedAt = TestSupport.Now,
            UpdatedAt = TestSupport.Now
        };
    }

    [Fact]
    public async Task CreateProduct_PersistsVariationsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        var categories = new Mock<ICategoryRepository>();
        var products = new Mock<IProductRepository>();
        Product? persisted = null;
        categories.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        products.Setup(value => value.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((value, _) => persisted = value)
            .Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        var useCase = new CreateProductUseCase(
            products.Object, categories.Object, new FixedClock(TestSupport.Now), unitOfWork.Object);

        var variations = new List<ProductVariationRequest>
        {
            new(null, "Copa", 5000m, 1, true),
            new(null, "Botella", 40000m, 2, true)
        };

        var result = await useCase.ExecuteAsync(
            tenantId,
            Guid.NewGuid(),
            "admin@saviaup.test",
            new CreateProductRequest(
                "NORMAL", "Ron Viejo de Caldas", category.Id, 5000m, null, null, null, false, null, variations),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(2, persisted.Variations.Count);
        Assert.Contains(persisted.Variations, v => v.Name == "Copa" && v.SalePrice == 5000m && v.NormalizedName == "COPA");
        Assert.Contains(persisted.Variations, v => v.Name == "Botella" && v.SalePrice == 40000m && v.NormalizedName == "BOTELLA");
        Assert.Equal(2, result.Value!.Variations.Count);
    }

    [Fact]
    public async Task UpdateProduct_ReplacesVariationsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        var existing = Product(tenantId);
        var categories = new Mock<ICategoryRepository>();
        var products = new Mock<IProductRepository>();
        IEnumerable<ProductVariation>? addedVariations = null;

        categories.Setup(value => value.GetByIdAsNoTrackingAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        products.Setup(value => value.GetByIdForUpdateAsync(tenantId, existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        products.Setup(value => value.DeleteVariationsAsync(tenantId, existing.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        products.Setup(value => value.AddVariationsAsync(It.IsAny<IEnumerable<ProductVariation>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ProductVariation>, CancellationToken>((vars, _) => addedVariations = vars)
            .Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        var useCase = new UpdateProductUseCase(
            products.Object, categories.Object, new FixedClock(TestSupport.Now), unitOfWork.Object);

        var newVariations = new List<ProductVariationRequest>
        {
            new(null, "Media", 22000m, 1, true)
        };

        var result = await useCase.ExecuteAsync(
            tenantId,
            existing.Id,
            Guid.NewGuid(),
            "admin@saviaup.test",
            new UpdateProductRequest(
                "NORMAL", "Ron Viejo de Caldas Actualizado", category.Id, 22000m, null, null, null, false, null, newVariations),
            default);

        Assert.True(result.IsSuccess);
        products.Verify(v => v.DeleteVariationsAsync(tenantId, existing.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(addedVariations);
        Assert.Single(addedVariations);
        Assert.Equal("Media", addedVariations.First().Name);
        Assert.Equal(22000m, addedVariations.First().SalePrice);
    }
}
