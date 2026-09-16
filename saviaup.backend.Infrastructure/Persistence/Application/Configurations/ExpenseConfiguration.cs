using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ConsecutiveNumber).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.IsCashOut).IsRequired();
        builder.Property(x => x.PaymentMethod).HasMaxLength(120).IsRequired();

        builder.Property(x => x.SupplierId);
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.ExpenseDate).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();

        builder.Property(x => x.AnnulledReason).HasMaxLength(500);
        builder.Property(x => x.AnnulledAt);
        builder.Property(x => x.AnnulledByUserId);
        builder.Property(x => x.AnnulledByUserName).HasMaxLength(200);

        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastModifiedByUserId).IsRequired();
        builder.Property(x => x.LastModifiedByUserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ConsecutiveNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.ExpenseDate });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
