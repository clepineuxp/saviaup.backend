using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class DiningAreaConfiguration : IEntityTypeConfiguration<DiningArea>
{
    public void Configure(EntityTypeBuilder<DiningArea> builder)
    {
        builder.ToTable("dining_areas", table =>
            table.HasCheckConstraint("CK_dining_areas_Order_Positive", "\"Order\" > 0"));
        builder.HasKey(area => area.Id);
        builder.Property(area => area.Name).HasMaxLength(120).IsRequired();
        builder.Property(area => area.NormalizedName).HasMaxLength(120).IsRequired();
        builder.HasIndex(area => new { area.TenantId, area.NormalizedName }).IsUnique();
        builder.HasIndex(area => new { area.TenantId, area.Order }).IsUnique();
        builder.HasIndex(area => new { area.TenantId, area.IsActive });
        builder.HasOne(area => area.Tenant)
            .WithMany(tenant => tenant.DiningAreas)
            .HasForeignKey(area => area.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
