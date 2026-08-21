using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

internal sealed class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("cash_registers");

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.Id)
            .HasColumnName("id");

        builder.Property(cr => cr.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(cr => cr.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(cr => cr.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(cr => cr.Location)
            .HasColumnName("location")
            .HasMaxLength(200);

        builder.Property(cr => cr.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(cr => cr.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(cr => cr.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(cr => cr.TenantId)
            .HasDatabaseName("ix_cash_registers_tenant_id");

        builder.HasIndex(cr => new { cr.TenantId, cr.NormalizedName })
            .IsUnique()
            .HasDatabaseName("ux_cash_registers_tenant_normalized_name");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(cr => cr.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
