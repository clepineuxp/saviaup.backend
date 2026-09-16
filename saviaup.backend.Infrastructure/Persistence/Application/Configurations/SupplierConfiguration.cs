using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.CommercialName).HasMaxLength(160);
        builder.Property(x => x.Document).HasMaxLength(80);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastModifiedByUserId).IsRequired();
        builder.Property(x => x.LastModifiedByUserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
