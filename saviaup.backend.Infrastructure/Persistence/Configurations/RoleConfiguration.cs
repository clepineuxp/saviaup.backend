using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Code).HasMaxLength(80).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(500);
        builder.HasIndex(role => new { role.TenantId, role.Code }).IsUnique();
        builder.HasIndex(role => role.TenantId);
        builder.HasOne(role => role.Tenant).WithMany(tenant => tenant.Roles).HasForeignKey(role => role.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
