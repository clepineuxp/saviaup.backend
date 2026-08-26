using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint("CK_products_SalePrice_Positive", "\"SalePrice\" > 0");
            table.HasCheckConstraint("CK_products_Type", "\"Type\" IN ('NORMAL', 'COMBO')");
            table.HasCheckConstraint(
                "CK_products_PreparationTime_NonNegative",
                "\"PreparationTimeMinutes\" IS NULL OR \"PreparationTimeMinutes\" >= 0");
        });
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Type)
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<ProductType>(value, true))
            .HasDefaultValue(ProductType.Normal)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(product => product.Name).HasMaxLength(120).IsRequired();
        builder.Property(product => product.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(1000);
        builder.Property(product => product.ImageRef);
        builder.HasOne(product => product.ImageStored)
            .WithMany()
            .HasForeignKey(product => product.ImageRef)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(product => product.SalePrice).HasPrecision(18, 2);
        builder.Property(product => product.CreatedByUserName).HasMaxLength(200);
        builder.Property(product => product.LastModifiedByUserName).HasMaxLength(200);
        builder.HasIndex(product => new { product.TenantId, product.IsActive, product.NormalizedName });
        builder.HasIndex(product => new { product.TenantId, product.CategoryId });
        builder.HasIndex(product => new { product.TenantId, product.Type });
        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
