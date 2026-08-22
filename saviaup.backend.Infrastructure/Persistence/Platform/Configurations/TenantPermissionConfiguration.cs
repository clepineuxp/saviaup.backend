using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform.Configurations;

public sealed class TenantPermissionConfiguration : IEntityTypeConfiguration<TenantPermission>
{
    public void Configure(EntityTypeBuilder<TenantPermission> builder)
    {
        builder.ToTable("tenant_permissions");
        builder.HasKey(item => new { item.TenantId, item.PermissionId });
        builder.HasIndex(item => item.PermissionId);
        builder.HasOne(item => item.Tenant).WithMany(tenant => tenant.EnabledPermissions)
            .HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Permission).WithMany(permission => permission.TenantPermissions)
            .HasForeignKey(item => item.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
