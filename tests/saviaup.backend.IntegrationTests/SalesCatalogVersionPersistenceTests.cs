using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class SalesCatalogVersionPersistenceTests
{
    [Fact]
    public async Task SavingCatalogEntities_CreatesAndUpdatesTenantVersion()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId);
        var category = Category(tenantId);
        context.Categories.Add(category);

        await context.SaveChangesAsync();

        var parameter = await CatalogVersion(context, tenantId);
        Assert.Equal("datetime", parameter.ValueType);

        parameter.Value = DateTimeOffset.UnixEpoch.ToString("O");
        await context.SaveChangesAsync();
        category.Name = "Bebidas frías";
        category.NormalizedName = "BEBIDAS FRIAS";

        await context.SaveChangesAsync();

        Assert.NotEqual(DateTimeOffset.UnixEpoch.ToString("O"), parameter.Value);
    }

    [Fact]
    public async Task TableOperationChanges_DoNotInvalidateCatalog_ButDisablingDoes()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId);
        var area = Area(tenantId);
        var table = Table(tenantId, area.Id);
        area.Tables.Add(table);
        context.DiningAreas.Add(area);
        await context.SaveChangesAsync();
        var parameter = await CatalogVersion(context, tenantId);
        var sentinel = DateTimeOffset.UnixEpoch.ToString("O");
        parameter.Value = sentinel;
        parameter.UpdatedAt = DateTimeOffset.UnixEpoch;
        await context.SaveChangesAsync();

        table.Status = TableStatus.Occupied;
        table.ActiveOrderId = Guid.NewGuid();
        table.ActiveOrderTotal = 25000;
        table.OccupiedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        Assert.Equal(sentinel, parameter.Value);

        table.Status = TableStatus.Disabled;
        table.ActiveOrderId = null;
        table.ActiveOrderTotal = 0;
        table.OccupiedAt = null;
        await context.SaveChangesAsync();

        Assert.NotEqual(sentinel, parameter.Value);
    }

    private static ApplicationDbContext CreateContext(Guid tenantId)
        => new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new TenantScope(tenantId));

    private static Task<OrganizationParameter> CatalogVersion(
        ApplicationDbContext context,
        Guid tenantId)
        => context.OrganizationParameters.IgnoreQueryFilters().SingleAsync(parameter =>
            parameter.TenantId == tenantId
            && parameter.Key == OrganizationParameterKeys.SalesCatalogLastModifiedAt);

    private static Category Category(Guid tenantId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Bebidas",
            NormalizedName = "BEBIDAS",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static DiningArea Area(Guid tenantId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Principal",
            NormalizedName = "PRINCIPAL",
            Order = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static RestaurantTable Table(Guid tenantId, Guid areaId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DiningAreaId = areaId,
            Name = "Mesa 1",
            NormalizedName = "MESA 1",
            Capacity = 4,
            Status = TableStatus.Available,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private sealed class TenantScope(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public bool HasTenant => true;
    }
}
