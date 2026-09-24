using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    public Guid TenantId => tenantContext.TenantId;

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DiningArea> DiningAreas => Set<DiningArea>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashRegisterShift> CashRegisterShifts => Set<CashRegisterShift>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderReceipt> OrderReceipts => Set<OrderReceipt>();
    public DbSet<OrganizationParameter> OrganizationParameters => Set<OrganizationParameter>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<StoredImage> StoredImages => Set<StoredImage>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ProductRecipeItem> ProductRecipeItems => Set<ProductRecipeItem>();
    public DbSet<ProductVariation> ProductVariations => Set<ProductVariation>();
    public DbSet<ProductComboGroup> ProductComboGroups => Set<ProductComboGroup>();
    public DbSet<ProductComboOption> ProductComboOptions => Set<ProductComboOption>();
    public DbSet<OrderItemComboSelection> OrderItemComboSelections => Set<OrderItemComboSelection>();
    public DbSet<DigitalMenuItem> DigitalMenuItems => Set<DigitalMenuItem>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<PrintAgent> PrintAgents => Set<PrintAgent>();
    public DbSet<PrintAgentCredential> PrintAgentCredentials => Set<PrintAgentCredential>();
    public DbSet<PrintAgentPairingCode> PrintAgentPairingCodes => Set<PrintAgentPairingCode>();
    public DbSet<PrintAgentDiscovery> PrintAgentDiscoveries => Set<PrintAgentDiscovery>();
    public DbSet<PrintAgentDiscoveredPrinter> PrintAgentDiscoveredPrinters => Set<PrintAgentDiscoveredPrinter>();
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<PrintingZone> PrintingZones => Set<PrintingZone>();
    public DbSet<PrintingZonePrinter> PrintingZonePrinters => Set<PrintingZonePrinter>();
    public DbSet<CategoryPrintingRoute> CategoryPrintingRoutes => Set<CategoryPrintingRoute>();
    public DbSet<ProductPrintingRoute> ProductPrintingRoutes => Set<ProductPrintingRoute>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            type => type.Namespace == "SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations");

        // Aislamiento Automático por Tenant (Global Query Filters)
        modelBuilder.Entity<Role>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Category>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<MeasurementUnit>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Ingredient>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<InventoryMovement>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<ProductRecipeItem>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<ProductVariation>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<ProductComboGroup>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<ProductComboOption>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<OrderItemComboSelection>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<DiningArea>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<RestaurantTable>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<CashRegister>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<CashRegisterShift>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Order>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<OrderReceipt>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<OrganizationParameter>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PaymentMethod>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<StoredImage>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Supplier>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Expense>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(x => x.Role.TenantId == TenantId);
        modelBuilder.Entity<DigitalMenuItem>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Location>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PrintAgent>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PrintAgentCredential>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PrintAgentPairingCode>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PrintAgentDiscoveredPrinter>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Printer>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PrintingZone>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PrintingZonePrinter>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<CategoryPrintingRoute>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<ProductPrintingRoute>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<PrintJob>().HasQueryFilter(x => x.TenantId == TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureTenantIdOnAddedEntities();
        await TouchSalesCatalogVersionAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task TouchSalesCatalogVersionAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        var tenantIds = ChangeTracker.Entries()
            .Where(RequiresSalesCatalogInvalidation)
            .Select(GetTenantId)
            .Where(tenantId => tenantId != Guid.Empty)
            .Distinct()
            .ToArray();
        if (tenantIds.Length == 0) return;

        var trackedParameters = ChangeTracker.Entries<OrganizationParameter>()
            .Where(entry => entry.State != EntityState.Deleted
                && entry.Entity.Key == OrganizationParameterKeys.SalesCatalogLastModifiedAt
                && tenantIds.Contains(entry.Entity.TenantId))
            .ToDictionary(entry => entry.Entity.TenantId, entry => entry.Entity);
        var missingTenantIds = tenantIds.Where(tenantId => !trackedParameters.ContainsKey(tenantId)).ToArray();
        if (missingTenantIds.Length > 0)
        {
            var storedParameters = await OrganizationParameters
                .IgnoreQueryFilters()
                .Where(parameter => missingTenantIds.Contains(parameter.TenantId)
                    && parameter.Key == OrganizationParameterKeys.SalesCatalogLastModifiedAt)
                .ToArrayAsync(cancellationToken);
            foreach (var parameter in storedParameters) trackedParameters[parameter.TenantId] = parameter;
        }

        var now = DateTimeOffset.UtcNow;
        var value = now.ToString("O", CultureInfo.InvariantCulture);
        foreach (var tenantId in tenantIds)
        {
            if (trackedParameters.TryGetValue(tenantId, out var parameter))
            {
                parameter.Value = value;
                parameter.ValueType = "datetime";
                parameter.UpdatedAt = now;
                continue;
            }

            OrganizationParameters.Add(new OrganizationParameter
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = OrganizationParameterKeys.SalesCatalogLastModifiedAt,
                Value = value,
                ValueType = "datetime",
                CreatedAt = now,
                UpdatedAt = now
            });
        }
    }

    private static bool RequiresSalesCatalogInvalidation(EntityEntry entry)
        => entry.Entity switch
        {
            Category or Product or ProductVariation or ProductComboGroup or ProductComboOption or DiningArea
                => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted,
            RestaurantTable
                => RequiresTableCatalogInvalidation(entry),
            _ => false
        };

    private static bool RequiresTableCatalogInvalidation(EntityEntry entry)
    {
        if (entry.State is EntityState.Added or EntityState.Deleted) return true;
        if (entry.State != EntityState.Modified) return false;

        string[] catalogProperties =
        [
            nameof(RestaurantTable.DiningAreaId),
            nameof(RestaurantTable.Name),
            nameof(RestaurantTable.NormalizedName),
            nameof(RestaurantTable.Capacity),
            nameof(RestaurantTable.PositionX),
            nameof(RestaurantTable.PositionY),
            nameof(RestaurantTable.Shape),
            nameof(RestaurantTable.IsDelivery),
            nameof(RestaurantTable.IsCashRegister)
        ];
        if (catalogProperties.Any(propertyName => entry.Property(propertyName).IsModified)) return true;

        var status = entry.Property(nameof(RestaurantTable.Status));
        return status.IsModified
            && ((TableStatus)status.OriginalValue! == TableStatus.Disabled
                || (TableStatus)status.CurrentValue! == TableStatus.Disabled);
    }

    private static Guid GetTenantId(EntityEntry entry)
        => entry.Property(nameof(OrganizationParameter.TenantId)).CurrentValue as Guid? ?? Guid.Empty;

    private void EnsureTenantIdOnAddedEntities()
    {
        if (TenantId == Guid.Empty) return;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;

            var tenantIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
            if (tenantIdProp != null)
            {
                var currentVal = tenantIdProp.CurrentValue as Guid? ?? Guid.Empty;
                if (currentVal == Guid.Empty)
                {
                    tenantIdProp.CurrentValue = TenantId;
                }
            }
        }
    }
}
