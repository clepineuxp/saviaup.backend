using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class ProductComboOptionConfiguration : IEntityTypeConfiguration<ProductComboOption>
{
    public void Configure(EntityTypeBuilder<ProductComboOption> builder)
    {
        builder.ToTable("product_combo_options", table =>
        {
            table.HasCheckConstraint("CK_product_combo_options_ProductQuantity", "\"ProductQuantity\" >= 1");
        });
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Id).ValueGeneratedNever();
        builder.Property(option => option.PriceAdjustment).HasPrecision(18, 2);
        builder.HasIndex(option => new { option.TenantId, option.ComboGroupId, option.Order });
        builder.HasIndex(option => new { option.ComboGroupId, option.ProductId })
            .IsUnique()
            .HasFilter("\"ProductVariationId\" IS NULL");
        builder.HasIndex(option => new { option.ComboGroupId, option.ProductVariationId })
            .IsUnique()
            .HasFilter("\"ProductVariationId\" IS NOT NULL");
        builder.HasOne(option => option.ComboGroup)
            .WithMany(group => group.Options)
            .HasForeignKey(option => option.ComboGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(option => option.Product)
            .WithMany()
            .HasForeignKey(option => option.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(option => option.ProductVariation)
            .WithMany()
            .HasForeignKey(option => option.ProductVariationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
