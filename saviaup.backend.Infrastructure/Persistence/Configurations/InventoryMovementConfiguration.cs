using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Direction).HasMaxLength(20).IsRequired();
        builder.Property(movement => movement.Reason).HasMaxLength(30).IsRequired();
        builder.Property(movement => movement.Quantity).HasPrecision(18, 3);
        builder.Property(movement => movement.StockBefore).HasPrecision(18, 3);
        builder.Property(movement => movement.StockAfter).HasPrecision(18, 3);
        builder.Property(movement => movement.Note).HasMaxLength(500);
        builder.HasIndex(movement => new { movement.TenantId, movement.CreatedAt });
        builder.HasIndex(movement => new { movement.IngredientId, movement.CreatedAt });
        builder.HasOne(movement => movement.Tenant)
            .WithMany(tenant => tenant.InventoryMovements)
            .HasForeignKey(movement => movement.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(movement => movement.Ingredient)
            .WithMany(ingredient => ingredient.Movements)
            .HasForeignKey(movement => movement.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(movement => movement.CreatedByUser)
            .WithMany(user => user.InventoryMovements)
            .HasForeignKey(movement => movement.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
