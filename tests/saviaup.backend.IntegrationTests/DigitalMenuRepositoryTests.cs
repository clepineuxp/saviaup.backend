using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;
using SaviaUp.Backend.Infrastructure.Persistence.Repositories;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class DigitalMenuRepositoryTests
{
    [Fact]
    public async Task PublicMenu_IncludesOrderedComboGroupsOptionsAndPriceAdjustments()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var comboId = Guid.NewGuid();
        var baseProductId = Guid.NewGuid();
        var variationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var platform = new PlatformDbContext(
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase($"digital-menu-platform-{Guid.NewGuid():N}")
                .Options);
        await using var application = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"digital-menu-app-{Guid.NewGuid():N}")
                .Options,
            new TenantScope(tenantId));

        platform.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Restaurante Demo",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await platform.SaveChangesAsync();

        application.OrganizationParameters.AddRange(
            Parameter(tenantId, OrganizationParameterKeys.DigitalMenuSlug, "demo", now),
            Parameter(tenantId, OrganizationParameterKeys.EnableDigitalMenu, "true", now));
        application.Categories.Add(new Category
        {
            Id = categoryId,
            TenantId = tenantId,
            Name = "Combos",
            NormalizedName = "COMBOS",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        var baseProduct = new Product
        {
            Id = baseProductId,
            TenantId = tenantId,
            CategoryId = categoryId,
            Type = ProductType.Normal,
            Name = "Hamburguesa",
            NormalizedName = "HAMBURGUESA",
            SalePrice = null,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var variation = new ProductVariation
        {
            Id = variationId,
            TenantId = tenantId,
            ProductId = baseProductId,
            Name = "Doble carne",
            NormalizedName = "DOBLE CARNE",
            SalePrice = 18000,
            Order = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var combo = new Product
        {
            Id = comboId,
            TenantId = tenantId,
            CategoryId = categoryId,
            Type = ProductType.Combo,
            Name = "Combo especial",
            NormalizedName = "COMBO ESPECIAL",
            SalePrice = 25000,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var group = new ProductComboGroup
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ComboProductId = comboId,
            Name = "Fuerte",
            SelectionType = ProductComboSelectionType.Multiple,
            IsRequired = true,
            MinSelections = 1,
            MaxSelections = 2,
            Order = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        var option = new ProductComboOption
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ComboGroupId = group.Id,
            ProductId = baseProductId,
            ProductVariationId = variationId,
            ProductQuantity = 2,
            PriceAdjustment = 3500,
            Order = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        application.Products.AddRange(baseProduct, combo);
        application.ProductVariations.Add(variation);
        application.ProductComboGroups.Add(group);
        application.ProductComboOptions.Add(option);
        await application.SaveChangesAsync();

        var repository = new DigitalMenuRepository(platform, application);
        var menu = await repository.GetPublicMenuAsync("demo", CancellationToken.None);

        var publicCombo = Assert.Single(Assert.Single(menu!.Categories).Products, item => item.Id == comboId);
        var publicGroup = Assert.Single(publicCombo.ComboGroups);
        Assert.Equal("Fuerte", publicGroup.Name);
        Assert.Equal("MULTIPLE", publicGroup.SelectionType);
        Assert.Equal(1, publicGroup.MinSelections);
        Assert.Equal(2, publicGroup.MaxSelections);
        var publicOption = Assert.Single(publicGroup.Options);
        Assert.Equal("Hamburguesa", publicOption.ProductName);
        Assert.Equal("Doble carne", publicOption.ProductVariationName);
        Assert.Equal(2, publicOption.ProductQuantity);
        Assert.Equal(3500, publicOption.PriceAdjustment);
    }

    private static OrganizationParameter Parameter(
        Guid tenantId,
        string key,
        string value,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = key,
            Value = value,
            ValueType = "string",
            CreatedAt = now,
            UpdatedAt = now
        };

    private sealed class TenantScope(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public bool HasTenant => true;
    }
}
