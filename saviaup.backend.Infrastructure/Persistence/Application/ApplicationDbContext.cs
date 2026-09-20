using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

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
    public DbSet<DigitalMenuItem> DigitalMenuItems => Set<DigitalMenuItem>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<PrintAgent> PrintAgents => Set<PrintAgent>();
    public DbSet<PrintAgentCredential> PrintAgentCredentials => Set<PrintAgentCredential>();
    public DbSet<PrintAgentPairingCode> PrintAgentPairingCodes => Set<PrintAgentPairingCode>();
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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureTenantIdOnAddedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

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
