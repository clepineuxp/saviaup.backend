using Moq;
using SaviaUp.Backend.Core.Categories;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class CategoryUseCaseTests
{
    [Fact]
    public async Task CreateCategory_NormalizesAndPersistsTenantData()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ICategoryRepository>();
        Category? persisted = null;
        repository.Setup(value => value.NameExistsAsync(
                tenantId,
                "BEBIDAS FRÍAS",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(value => value.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((category, _) => persisted = category)
            .Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        var useCase = new CreateCategoryUseCase(
            repository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            new CreateCategoryRequest(
                "  Bebidas   frías  ",
                "  Para la barra  ",
                " https://cdn.saviaup.test/categories/drinks.webp ",
                true),
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(tenantId, persisted.TenantId);
        Assert.Equal(("Bebidas frías", "BEBIDAS FRÍAS"), (persisted.Name, persisted.NormalizedName));
        Assert.Equal("Para la barra", persisted.Description);
        Assert.Equal("https://cdn.saviaup.test/categories/drinks.webp", persisted.ImageUrl);
        Assert.True(persisted.IsInventoryTracked);
        Assert.True(persisted.IsActive);
        Assert.Equal(TestSupport.Now, persisted.CreatedAt);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateNameInTenant_ReturnsConflict()
    {
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.NameExistsAsync(
                It.IsAny<Guid>(),
                "POSTRES",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var unitOfWork = UnitOfWork();
        var useCase = new CreateCategoryUseCase(
            repository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new CreateCategoryRequest(" postres ", null, null, false),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CategoryNameAlreadyExists, result.Error!.Code);
        repository.Verify(value => value.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCategory_WithNonHttpImageUrl_ReturnsValidationError()
    {
        var useCase = new CreateCategoryUseCase(
            Mock.Of<ICategoryRepository>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new CreateCategoryRequest("Bebidas", null, "file:///private/image.png", false),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateCategory_UpdatesEditableFieldsWithoutChangingStatus()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId, isActive: false);
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repository.Setup(value => value.NameExistsAsync(
                tenantId,
                "COMIDA RÁPIDA",
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var useCase = new UpdateCategoryUseCase(
            repository.Object,
            Mock.Of<IProductRepository>(),
            new FixedClock(TestSupport.Now),
            UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            category.Id,
            new UpdateCategoryRequest("Comida rápida", " ", null, true),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(("Comida rápida", "COMIDA RÁPIDA"), (category.Name, category.NormalizedName));
        Assert.Null(category.Description);
        Assert.True(category.IsInventoryTracked);
        Assert.False(category.IsActive);
        Assert.Equal(TestSupport.Now, category.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCategory_WithAnotherCategoryName_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repository.Setup(value => value.NameExistsAsync(
                tenantId,
                "POSTRES",
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var unitOfWork = UnitOfWork();
        var useCase = new UpdateCategoryUseCase(
            repository.Object,
            Mock.Of<IProductRepository>(),
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            category.Id,
            new UpdateCategoryRequest("Postres", null, null, false),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CategoryNameAlreadyExists, result.Error!.Code);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategory_WhenInventoryTrackingIsDisabled_DisablesItForProducts()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        category.IsInventoryTracked = true;
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(value => value.GetByIdAsync(
                tenantId,
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        categories.Setup(value => value.NameExistsAsync(
                tenantId,
                category.NormalizedName,
                category.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var products = new Mock<IProductRepository>();
        products.Setup(value => value.DisableInventoryTrackingByCategoryAsync(
                tenantId,
                category.Id,
                TestSupport.Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var useCase = new UpdateCategoryUseCase(
            categories.Object,
            products.Object,
            new FixedClock(TestSupport.Now),
            UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            category.Id,
            new UpdateCategoryRequest(category.Name, null, null, false),
            default);

        Assert.True(result.IsSuccess);
        products.Verify(value => value.DisableInventoryTrackingByCategoryAsync(
            tenantId,
            category.Id,
            TestSupport.Now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCategoryStatus_DisablesExistingCategory()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        var unitOfWork = UnitOfWork();
        var useCase = new SetCategoryStatusUseCase(
            repository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var result = await useCase.ExecuteAsync(
            tenantId,
            category.Id,
            new SetCategoryStatusRequest(false),
            default);

        Assert.True(result.IsSuccess);
        Assert.False(category.IsActive);
        Assert.Equal(TestSupport.Now, category.UpdatedAt);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_RemovesExistingTenantCategory()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        var unitOfWork = UnitOfWork();
        var useCase = new DeleteCategoryUseCase(repository.Object, unitOfWork.Object);

        var result = await useCase.ExecuteAsync(tenantId, category.Id, default);

        Assert.True(result.IsSuccess);
        repository.Verify(value => value.Remove(category), Times.Once);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_WhenCategoryDoesNotBelongToTenant_ReturnsNotFound()
    {
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        var unitOfWork = UnitOfWork();
        var useCase = new DeleteCategoryUseCase(repository.Object, unitOfWork.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CategoryNotFound, result.Error!.Code);
        repository.Verify(value => value.Remove(It.IsAny<Category>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategory_WhenUsedByIngredient_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var category = Category(tenantId);
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.GetByIdAsync(tenantId, category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        repository.Setup(value => value.IsInUseAsync(tenantId, category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var useCase = new DeleteCategoryUseCase(repository.Object, UnitOfWork().Object);

        var result = await useCase.ExecuteAsync(tenantId, category.Id, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CategoryInUse, result.Error!.Code);
        repository.Verify(value => value.Remove(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task ListCategories_UsesTenantAndInactiveFilter()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(value => value.GetForTenantAsync(tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Category(tenantId), Category(tenantId, isActive: false)]);
        var useCase = new ListCategoriesUseCase(repository.Object);

        var result = await useCase.ExecuteAsync(tenantId, true, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        repository.Verify(value => value.GetForTenantAsync(tenantId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private static Category Category(Guid tenantId, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Bebidas",
        NormalizedName = "BEBIDAS",
        IsActive = isActive,
        CreatedAt = TestSupport.Now.AddDays(-1),
        UpdatedAt = TestSupport.Now.AddDays(-1)
    };
}
