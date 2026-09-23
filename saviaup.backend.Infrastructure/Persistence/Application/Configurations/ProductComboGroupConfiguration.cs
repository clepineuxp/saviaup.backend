using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class ProductComboGroupConfiguration : IEntityTypeConfiguration<ProductComboGroup>
{
    public void Configure(EntityTypeBuilder<ProductComboGroup> builder)
    {
        builder.ToTable("product_combo_groups", table =>
        {
            table.HasCheckConstraint("CK_product_combo_groups_Limits", "\"MinSelections\" >= 0 AND \"MaxSelections\" >= 1 AND \"MinSelections\" <= \"MaxSelections\"");
            table.HasCheckConstraint("CK_product_combo_groups_SelectionType", "\"SelectionType\" IN ('SINGLE', 'MULTIPLE', 'FIXED')");
        });
        builder.HasKey(group => group.Id);
        builder.Property(group => group.Id).ValueGeneratedNever();
        builder.Property(group => group.Name).HasMaxLength(120).IsRequired();
        builder.Property(group => group.SelectionType)
            .HasConversion(value => value.ToString().ToUpperInvariant(), value => Enum.Parse<ProductComboSelectionType>(value, true))
            .HasMaxLength(10)
            .IsRequired();
        builder.HasIndex(group => new { group.TenantId, group.ComboProductId, group.Order });
        builder.HasOne(group => group.ComboProduct)
            .WithMany(product => product.ComboGroups)
            .HasForeignKey(group => group.ComboProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
