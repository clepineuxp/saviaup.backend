using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class DigitalMenuItemConfiguration : IEntityTypeConfiguration<DigitalMenuItem>
{
    public void Configure(EntityTypeBuilder<DigitalMenuItem> builder)
    {
        builder.ToTable("digital_menu_items", table =>
        {
            table.HasCheckConstraint("CK_digital_menu_items_ItemType", "\"ItemType\" IN ('CATEGORY', 'PRODUCT')");
        });

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.ItemType).HasMaxLength(20).IsRequired();
        builder.Property(item => item.TargetId).IsRequired();
        builder.Property(item => item.CategoryId);
        builder.Property(item => item.SortOrder).IsRequired();
        builder.Property(item => item.IsActive).HasDefaultValue(true);
        builder.Property(item => item.CreatedByUserName).HasMaxLength(200);
        builder.Property(item => item.LastModifiedByUserName).HasMaxLength(200);

        builder.HasIndex(item => new { item.TenantId, item.ItemType, item.SortOrder });
        builder.HasIndex(item => new { item.TenantId, item.TargetId });
        builder.HasIndex(item => new { item.TenantId, item.CategoryId });
    }
}
