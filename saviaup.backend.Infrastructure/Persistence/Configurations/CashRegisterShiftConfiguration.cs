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
        builder.HasOne(shift => shift.Tenant)
            .WithMany(tenant => tenant.CashRegisterShifts)
            .HasForeignKey(shift => shift.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(shift => shift.OpenedByUser)
            .WithMany()
            .HasForeignKey(shift => shift.OpenedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
