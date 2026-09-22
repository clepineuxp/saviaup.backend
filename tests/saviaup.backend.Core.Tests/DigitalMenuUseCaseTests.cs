using Moq;
using SaviaUp.Backend.Core.DigitalMenu;
using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class DigitalMenuUseCaseTests
{
    [Fact]
    public async Task DigitalMenu_SlugUniquenessAcrossTenantsIsEnforced()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Restaurante A" };
        var parameters = SettingsDefaults.CreateBusinessParameters(tenantId, TestSupport.Now).ToArray();

        var repository = new Mock<ISettingsRepository>();
        repository.Setup(r => r.GetTenantForUpdateAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repository.Setup(r => r.GetParametersAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(parameters);
        repository.Setup(r => r.DigitalMenuSlugExistsAsync("restaurante-a", tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var useCase = CreateUseCase(repository.Object);

        var result = await useCase.UpdateParametersAsync(
            tenantId,
            new UpdateDigitalMenuParametersRequest(Enabled: true, Slug: "restaurante-a"),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.DigitalMenuSlugAlreadyExists, result.Error!.Code);
    }

    [Fact]
    public async Task DigitalMenu_SlugImmutabilityOnceSetIsEnforced()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Restaurante A" };
        var parameters = SettingsDefaults.CreateBusinessParameters(tenantId, TestSupport.Now).ToList();
        var slugParam = parameters.First(p => p.Key == SettingsDefaults.DigitalMenuSlug);
        slugParam.Value = "original-slug";

        var repository = new Mock<ISettingsRepository>();
        repository.Setup(r => r.GetTenantForUpdateAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repository.Setup(r => r.GetParametersAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(parameters);

        var useCase = CreateUseCase(repository.Object);

        var result = await useCase.UpdateParametersAsync(
            tenantId,
            new UpdateDigitalMenuParametersRequest(Enabled: true, Slug: "new-slug"),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.DigitalMenuSlugImmutable, result.Error!.Code);
    }

    [Fact]
    public async Task DigitalMenu_EnableRequiresSlug()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Restaurante A" };
        var parameters = SettingsDefaults.CreateBusinessParameters(tenantId, TestSupport.Now).ToArray();

        var repository = new Mock<ISettingsRepository>();
        repository.Setup(r => r.GetTenantForUpdateAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repository.Setup(r => r.GetParametersAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(parameters);

        var useCase = CreateUseCase(repository.Object);

        var result = await useCase.UpdateParametersAsync(
            tenantId,
            new UpdateDigitalMenuParametersRequest(Enabled: true, Slug: null),
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.DigitalMenuSlugRequired, result.Error!.Code);
    }

    [Fact]
    public async Task DigitalMenu_GetConfigMapsCategoriesAndProductsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Restaurante" };
        var parameters = SettingsDefaults.CreateBusinessParameters(tenantId, TestSupport.Now).ToList();
        parameters.First(p => p.Key == SettingsDefaults.EnableDigitalMenu).Value = "true";
        parameters.First(p => p.Key == SettingsDefaults.DigitalMenuSlug).Value = "mi-restaurante";

        var catId = Guid.NewGuid();
        var prodId = Guid.NewGuid();

        var category = new Category { Id = catId, TenantId = tenantId, Name = "Bebidas", IsActive = true };
        var product = new Product { Id = prodId, TenantId = tenantId, CategoryId = catId, Name = "Limonada", SalePrice = 5000, IsActive = true };

        var settingsRepo = new Mock<ISettingsRepository>();
        settingsRepo.Setup(r => r.GetTenantForUpdateAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        settingsRepo.Setup(r => r.GetParametersAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(parameters);

        var digitalMenuRepo = new Mock<IDigitalMenuRepository>();
        digitalMenuRepo.Setup(r => r.GetItemsAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new DigitalMenuItem { Id = Guid.NewGuid(), TenantId = tenantId, ItemType = "CATEGORY", TargetId = catId, SortOrder = 1, IsActive = true },
            new DigitalMenuItem { Id = Guid.NewGuid(), TenantId = tenantId, ItemType = "PRODUCT", TargetId = prodId, CategoryId = catId, SortOrder = 1, IsActive = true }
        ]);

        var catRepo = new Mock<ICategoryRepository>();
        catRepo.Setup(r => r.GetForTenantAsync(tenantId, false, It.IsAny<CancellationToken>())).ReturnsAsync([category]);

        var prodRepo = new Mock<IProductRepository>();
        prodRepo.Setup(r => r.GetAllForTenantAsync(tenantId, false, It.IsAny<CancellationToken>())).ReturnsAsync([product]);

        var useCase = new DigitalMenuUseCase(
            settingsRepo.Object,
            digitalMenuRepo.Object,
            catRepo.Object,
            prodRepo.Object,
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.GetConfigAsync(tenantId, default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Enabled);
        Assert.Equal("mi-restaurante", result.Value.Slug);
        Assert.False(result.Value.CanEditSlug);
        Assert.Single(result.Value.Categories);
        Assert.Single(result.Value.Products);
        Assert.Equal("Bebidas", result.Value.Categories.First().Name);
        Assert.Equal("Limonada", result.Value.Products.First().Name);
    }

    [Fact]
    public async Task DigitalMenu_UpdateItemsReplacesBatchWithAuditing()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Restaurante" };

        var settingsRepo = new Mock<ISettingsRepository>();
        settingsRepo.Setup(r => r.GetTenantForUpdateAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        IEnumerable<DigitalMenuItem>? capturedItems = null;
        var digitalMenuRepo = new Mock<IDigitalMenuRepository>();
        digitalMenuRepo.Setup(r => r.ReplaceItemsAsync(tenantId, It.IsAny<IEnumerable<DigitalMenuItem>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IEnumerable<DigitalMenuItem>, CancellationToken>((t, items, c) => capturedItems = items)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();

        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(repository => repository.GetForTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Category { Id = categoryId, TenantId = tenantId, Name = "Bebidas", IsActive = true }]);
        var productRepository = new Mock<IProductRepository>();
        productRepository.Setup(repository => repository.GetAllForTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = productId, TenantId = tenantId, CategoryId = categoryId, Name = "Limonada", SalePrice = 5000, IsActive = true }]);

        var useCase = new DigitalMenuUseCase(
            settingsRepo.Object,
            digitalMenuRepo.Object,
            categoryRepository.Object,
            productRepository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var request = new SaveDigitalMenuItemsRequest(
        [
            new SaveDigitalMenuItemInput("CATEGORY", categoryId, null, 1, true),
            new SaveDigitalMenuItemInput("PRODUCT", productId, categoryId, 1, true)
        ]);

        var result = await useCase.UpdateItemsAsync(tenantId, request, userId, "Admin", default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedItems);
        Assert.Equal(2, capturedItems!.Count());
        Assert.All(capturedItems, item => Assert.Equal("Admin", item.CreatedByUserName));
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DigitalMenu_UpdateParametersPersistsSlugAndEnabledState()
    {
        var tenantId = Guid.NewGuid();
        var parameters = SettingsDefaults.CreateBusinessParameters(tenantId, TestSupport.Now).ToArray();
        var settingsRepository = new Mock<ISettingsRepository>();
        settingsRepository.Setup(repository => repository.GetTenantForUpdateAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Restaurante" });
        settingsRepository.Setup(repository => repository.GetParametersAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parameters);
        var unitOfWork = new Mock<IUnitOfWork>();
        var useCase = CreateUseCase(settingsRepository.Object, unitOfWork.Object);

        var result = await useCase.UpdateParametersAsync(
            tenantId,
            new UpdateDigitalMenuParametersRequest(Enabled: true, Slug: "Mi-Restaurante"),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("mi-restaurante", parameters.Single(parameter => parameter.Key == SettingsDefaults.DigitalMenuSlug).Value);
        Assert.Equal("true", parameters.Single(parameter => parameter.Key == SettingsDefaults.EnableDigitalMenu).Value);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DigitalMenu_GetCategoryImagesReturnsProgressiveBatch()
    {
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var expected = new PublicDigitalMenuCategoryImagesDto(
            categoryId,
            "data:image/webp;base64,CATEGORY",
            [new PublicDigitalMenuProductImageDto(productId, "data:image/webp;base64,PRODUCT")]);
        var repository = new Mock<IDigitalMenuRepository>();
        repository
            .Setup(item => item.GetPublicMenuCategoryImagesAsync(
                "mi-restaurante",
                categoryId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var useCase = new DigitalMenuUseCase(
            Mock.Of<ISettingsRepository>(),
            repository.Object,
            Mock.Of<ICategoryRepository>(),
            Mock.Of<IProductRepository>(),
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>());

        var result = await useCase.GetPublicMenuCategoryImagesAsync(
            "mi-restaurante",
            categoryId,
            default);

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
    }

    private static DigitalMenuUseCase CreateUseCase(ISettingsRepository settingsRepository, IUnitOfWork? unitOfWork = null)
        => new(
            settingsRepository,
            Mock.Of<IDigitalMenuRepository>(),
            Mock.Of<ICategoryRepository>(),
            Mock.Of<IProductRepository>(),
            new FixedClock(TestSupport.Now),
            unitOfWork ?? Mock.Of<IUnitOfWork>());
}
