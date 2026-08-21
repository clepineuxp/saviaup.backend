using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("restaurant_tables", table =>
        {
            table.HasCheckConstraint("CK_restaurant_tables_Capacity", "\"Capacity\" BETWEEN 1 AND 100");
            table.HasCheckConstraint("CK_restaurant_tables_PositionX", "\"PositionX\" BETWEEN -100000 AND 100000");
            table.HasCheckConstraint("CK_restaurant_tables_PositionY", "\"PositionY\" BETWEEN -100000 AND 100000");
            table.HasCheckConstraint(
                "CK_restaurant_tables_Shape",
                "\"Shape\" IN ('SQUARE', 'ROUND', 'RECTANGLEHORIZONTAL', 'RECTANGLEVERTICAL')");
            table.HasCheckConstraint("CK_restaurant_tables_ActiveOrderTotal", "\"ActiveOrderTotal\" >= 0");
        });
        builder.HasKey(table => table.Id);
        builder.Property(table => table.Name).HasMaxLength(120).IsRequired();
        builder.Property(table => table.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(table => table.PositionX).HasPrecision(18, 2);
        builder.Property(table => table.PositionY).HasPrecision(18, 2);
        builder.Property(table => table.Shape)
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<TableShape>(value, true))
            .HasMaxLength(30)
            .HasDefaultValue(TableShape.Square)
            .IsRequired();
        builder.Property(table => table.ActiveOrderTotal).HasPrecision(18, 2);
        builder.Property(table => table.Status)
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<TableStatus>(value, true))
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(table => new { table.TenantId, table.DiningAreaId, table.NormalizedName }).IsUnique();
        builder.HasIndex(table => new { table.TenantId, table.Status });
        builder.HasOne(table => table.Tenant)
            .WithMany(tenant => tenant.RestaurantTables)
            .HasForeignKey(table => table.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(table => table.DiningArea)
            .WithMany(area => area.Tables)
            .HasForeignKey(table => table.DiningAreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
