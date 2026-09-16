using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using DomainModuleEntity = SaviaUp.Backend.Domain.Entities.Module;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<DomainModuleEntity> Modules => Set<DomainModuleEntity>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<TenantPermission> TenantPermissions => Set<TenantPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PlatformDbContext).Assembly,
            type => type.Namespace == "SaviaUp.Backend.Infrastructure.Persistence.Platform.Configurations");
    }
}
