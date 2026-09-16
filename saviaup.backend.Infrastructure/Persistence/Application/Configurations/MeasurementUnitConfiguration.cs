using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.ToTable("measurement_units");
        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Code).HasMaxLength(20).IsRequired();
        builder.Property(unit => unit.NormalizedCode).HasMaxLength(20).IsRequired();
        builder.Property(unit => unit.Name).HasMaxLength(120).IsRequired();
        builder.Property(unit => unit.NormalizedName).HasMaxLength(120).IsRequired();
        builder.HasIndex(unit => new { unit.TenantId, unit.NormalizedCode }).IsUnique();
        builder.HasIndex(unit => new { unit.TenantId, unit.NormalizedName }).IsUnique();
        builder.HasIndex(unit => new { unit.TenantId, unit.IsActive, unit.Name });
    }
}
