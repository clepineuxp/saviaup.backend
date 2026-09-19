using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class ProductVariationConfiguration : IEntityTypeConfiguration<ProductVariation>
{
    public void Configure(EntityTypeBuilder<ProductVariation> builder)
    {
        builder.ToTable("product_variations", table =>
        {
            table.HasCheckConstraint("CK_product_variations_SalePrice_Positive", "\"SalePrice\" > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.Order).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => new { x.TenantId, x.ProductId });
        builder.HasIndex(x => new { x.TenantId, x.IsActive, x.NormalizedName });

        builder.HasOne(x => x.Product)
            .WithMany(p => p.Variations)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
