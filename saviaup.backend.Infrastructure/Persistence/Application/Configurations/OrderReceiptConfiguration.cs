using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class OrderReceiptConfiguration : IEntityTypeConfiguration<OrderReceipt>
{
    public void Configure(EntityTypeBuilder<OrderReceipt> builder)
    {
        builder.ToTable("order_receipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.OrderId)
            .IsRequired();

        builder.Property(r => r.ReceiptNumber)
            .IsRequired();

        builder.Property(r => r.ReceiptType)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(r => r.SubtotalAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.TipAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.PaymentMethod)
            .HasMaxLength(120);

        builder.Property(r => r.PaymentDetailsJson)
            .HasColumnType("text");

        builder.Property(r => r.ItemsJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(r => r.IssuedByUserId)
            .IsRequired();

        builder.Property(r => r.IssuedByUserName)
            .IsRequired()
            .HasMaxLength(160);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Order)
            .WithMany(o => o.Receipts)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.OrderId });
        builder.HasIndex(r => new { r.TenantId, r.ReceiptNumber });
    }
}
