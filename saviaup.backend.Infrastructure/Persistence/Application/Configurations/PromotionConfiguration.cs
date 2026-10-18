using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions", table =>
        {
            table.HasCheckConstraint("CK_promotions_Value_Positive", "\"Value\" > 0");
            table.HasCheckConstraint("CK_promotions_WeekDaysMask_Valid", "\"WeekDaysMask\" >= 0 AND \"WeekDaysMask\" <= 127");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Type)
            .HasConversion(value => value.ToString().ToUpperInvariant(), value => Enum.Parse<PromotionType>(value, true))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.ValueType)
            .HasConversion(value => value.ToString().ToUpperInvariant(), value => Enum.Parse<PromotionValueType>(value, true))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.Property(x => x.StartTime).HasColumnType("time without time zone");
        builder.Property(x => x.EndTime).HasColumnType("time without time zone");
        builder.Property(x => x.SpecificDate).HasColumnType("date");
        builder.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
        builder.HasMany(x => x.Targets).WithOne(x => x.Promotion).HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
    }
}
