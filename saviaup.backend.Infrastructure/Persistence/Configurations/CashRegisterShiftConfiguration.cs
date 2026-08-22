using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class CashRegisterShiftConfiguration : IEntityTypeConfiguration<CashRegisterShift>
{
    public void Configure(EntityTypeBuilder<CashRegisterShift> builder)
    {
        builder.ToTable("cash_register_shifts");
        builder.HasKey(shift => shift.Id);
        builder.HasIndex(shift => new { shift.TenantId, shift.ClosedAt });
        builder.HasIndex(shift => new { shift.TenantId, shift.CashRegisterId, shift.Status });

        builder.Property(shift => shift.Status).HasMaxLength(20).HasDefaultValue("OPEN");
        builder.Property(shift => shift.OpenedByUserName).HasMaxLength(200);
        builder.Property(shift => shift.ClosedByUserName).HasMaxLength(200);
        builder.Property(shift => shift.OpeningBalancesJson).HasColumnType("text").HasDefaultValue("[]");
        builder.Property(shift => shift.ClosingSummaryJson).HasColumnType("text");

        builder.Property(shift => shift.TotalSalesAmount).HasPrecision(18, 2);
        builder.Property(shift => shift.TotalTipsAmount).HasPrecision(18, 2);
        builder.Property(shift => shift.TotalCollectedAmount).HasPrecision(18, 2);
        builder.Property(shift => shift.TotalExpensesAmount).HasPrecision(18, 2);

        builder.HasOne(shift => shift.Tenant)
            .WithMany(tenant => tenant.CashRegisterShifts)
            .HasForeignKey(shift => shift.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(shift => shift.CashRegister)
            .WithMany()
            .HasForeignKey(shift => shift.CashRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(shift => shift.OpenedByUser)
            .WithMany()
            .HasForeignKey(shift => shift.OpenedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(shift => shift.ClosedByUser)
            .WithMany()
            .HasForeignKey(shift => shift.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
