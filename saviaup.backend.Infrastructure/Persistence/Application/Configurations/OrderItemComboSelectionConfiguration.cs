using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class OrderItemComboSelectionConfiguration : IEntityTypeConfiguration<OrderItemComboSelection>
{
    public void Configure(EntityTypeBuilder<OrderItemComboSelection> builder)
    {
        builder.ToTable("order_item_combo_selections", table =>
        {
            table.HasCheckConstraint("CK_order_item_combo_selections_Quantities", "\"ProductQuantity\" >= 1 AND \"SelectionQuantity\" >= 1");
        });
        builder.HasKey(selection => selection.Id);
        builder.Property(selection => selection.Id).ValueGeneratedNever();
        builder.Property(selection => selection.GroupName).HasMaxLength(120).IsRequired();
        builder.Property(selection => selection.ProductName).HasMaxLength(160).IsRequired();
        builder.Property(selection => selection.PriceAdjustment).HasPrecision(18, 2);
        builder.HasIndex(selection => new { selection.TenantId, selection.OrderItemId });
        builder.HasOne(selection => selection.OrderItem)
            .WithMany(item => item.ComboSelections)
            .HasForeignKey(selection => selection.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(selection => selection.ComboGroup)
            .WithMany()
            .HasForeignKey(selection => selection.ComboGroupId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(selection => selection.ComboOption)
            .WithMany()
            .HasForeignKey(selection => selection.ComboOptionId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(selection => selection.Product)
            .WithMany()
            .HasForeignKey(selection => selection.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
