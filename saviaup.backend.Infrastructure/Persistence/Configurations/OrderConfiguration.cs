using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status).HasMaxLength(30).IsRequired();
        builder.Property(o => o.SubtotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.TaxAmount).HasPrecision(18, 2);
        builder.Property(o => o.TipAmount).HasPrecision(18, 2);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.PaymentMethod).HasMaxLength(120);
        builder.Property(o => o.PaymentDetailsJson).HasMaxLength(4000);
        builder.Property(o => o.Observations).HasMaxLength(500);
        builder.Property(o => o.CreatedByUserName).HasMaxLength(160).IsRequired();
        builder.Property(o => o.LastModifiedByUserName).HasMaxLength(160);
        builder.Property(o => o.PaidByUserName).HasMaxLength(160);

        builder.HasIndex(o => new { o.TenantId, o.TableId, o.Status });
        builder.HasIndex(o => new { o.TenantId, o.OrderNumber });

        builder.HasOne(o => o.Tenant)
            .WithMany()
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Table)
            .WithMany()
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.CashRegisterShift)
            .WithMany()
            .HasForeignKey(o => o.CashRegisterShiftId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
