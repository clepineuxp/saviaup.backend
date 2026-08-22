using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName).HasMaxLength(160).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        builder.Property(i => i.Status).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.CancellationReason).HasMaxLength(300);
        builder.Property(i => i.CancelledByUserName).HasMaxLength(160);
        builder.Property(i => i.CreatedByUserName).HasMaxLength(160).IsRequired();
        builder.Property(i => i.LastModifiedByUserName).HasMaxLength(160);

        builder.HasIndex(i => i.OrderId);

        builder.HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
