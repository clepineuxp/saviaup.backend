using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class ProductRecipeItemConfiguration : IEntityTypeConfiguration<ProductRecipeItem>
{
    public void Configure(EntityTypeBuilder<ProductRecipeItem> builder)
    {
        builder.ToTable("product_recipe_items", table =>
        {
            table.HasCheckConstraint("CK_product_recipe_items_Quantity_Positive", "\"Quantity\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CustomIngredientName).HasMaxLength(160);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Order).HasDefaultValue(0);

        builder.HasIndex(x => new { x.TenantId, x.ProductId });
        builder.HasIndex(x => new { x.TenantId, x.IngredientId });

        builder.HasOne(x => x.Product)
            .WithMany(p => p.RecipeItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Ingredient)
            .WithMany()
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
