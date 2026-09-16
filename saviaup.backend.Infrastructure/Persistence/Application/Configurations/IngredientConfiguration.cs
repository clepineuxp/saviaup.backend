using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients");
        builder.HasKey(ingredient => ingredient.Id);
        builder.Property(ingredient => ingredient.Name).HasMaxLength(120).IsRequired();
        builder.Property(ingredient => ingredient.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(ingredient => ingredient.Description).HasMaxLength(1000);
        builder.Property(ingredient => ingredient.MinimumStock).HasPrecision(18, 3);
        builder.Property(ingredient => ingredient.CurrentStock).HasPrecision(18, 3);
        builder.HasIndex(ingredient => new { ingredient.TenantId, ingredient.IsActive, ingredient.NormalizedName });
        builder.HasIndex(ingredient => new { ingredient.TenantId, ingredient.CategoryId });
        builder.HasIndex(ingredient => new { ingredient.TenantId, ingredient.MeasurementUnitId });
        builder.HasOne(ingredient => ingredient.Category)
            .WithMany(category => category.Ingredients)
            .HasForeignKey(ingredient => ingredient.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ingredient => ingredient.MeasurementUnit)
            .WithMany(unit => unit.Ingredients)
            .HasForeignKey(ingredient => ingredient.MeasurementUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
