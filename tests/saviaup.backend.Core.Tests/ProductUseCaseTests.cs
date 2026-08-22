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
            new CreateProductRequest("COMBO", "Combo familiar", category.Id, 50000m, null, null, null, true),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductType.Combo, persisted!.Type);
        Assert.True(persisted.IsInventoryTracked);
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
        products.Setup(value => value.GetByIdAsync(tenantId, product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        categories.Setup(value => value.GetByIdAsync(tenantId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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
}
