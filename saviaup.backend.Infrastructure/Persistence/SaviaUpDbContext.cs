using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using DomainModuleEntity = SaviaUp.Backend.Domain.Entities.Module;

namespace SaviaUp.Backend.Infrastructure.Persistence;

public sealed class SaviaUpDbContext(DbContextOptions<SaviaUpDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<DomainModuleEntity> Modules => Set<DomainModuleEntity>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DiningArea> DiningAreas => Set<DiningArea>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<CashRegisterShift> CashRegisterShifts => Set<CashRegisterShift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaviaUpDbContext).Assembly);
}
