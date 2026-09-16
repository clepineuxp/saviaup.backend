using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Name).HasMaxLength(120).IsRequired();
        builder.Property(tenant => tenant.ResponsibleName).HasMaxLength(160);
        builder.Property(tenant => tenant.Document).HasMaxLength(80);
        builder.Property(tenant => tenant.ContactName).HasMaxLength(160);
        builder.Property(tenant => tenant.Email).HasMaxLength(320);
        builder.Property(tenant => tenant.Address).HasMaxLength(500);
        builder.Property(tenant => tenant.Country).HasMaxLength(100);
        builder.Property(tenant => tenant.State).HasMaxLength(120);
        builder.Property(tenant => tenant.City).HasMaxLength(120);
        builder.Property(tenant => tenant.Phone).HasMaxLength(50);
        builder.Property(tenant => tenant.Website).HasMaxLength(2048);
        builder.Property(tenant => tenant.LogoContentType).HasMaxLength(100);
        builder.Property(tenant => tenant.LogoFileName).HasMaxLength(255);
        builder.Property(tenant => tenant.RequiresOpenCashRegister).HasDefaultValue(false);
        builder.HasIndex(tenant => tenant.IsActive);
    }
}
