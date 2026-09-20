using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsDefault });
    }
}

public sealed class PrintAgentConfiguration : IEntityTypeConfiguration<PrintAgent>
{
    public void Configure(EntityTypeBuilder<PrintAgent> builder)
    {
        builder.ToTable("print_agents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.DeviceIdentifier).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Hostname).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OperatingSystem).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LocalIpAddress).HasMaxLength(64);
        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.DeviceIdentifier }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Enabled, x.LastSeenAt });
        builder.HasOne(x => x.Location).WithMany(x => x.PrintAgents).HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PrintAgentCredentialConfiguration : IEntityTypeConfiguration<PrintAgentCredential>
{
    public void Configure(EntityTypeBuilder<PrintAgentCredential> builder)
    {
        builder.ToTable("print_agent_credentials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.PrintAgentId, x.ExpiresAt });
        builder.HasOne(x => x.PrintAgent).WithMany(x => x.Credentials).HasForeignKey(x => x.PrintAgentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PrintAgentDiscoveredPrinterConfiguration : IEntityTypeConfiguration<PrintAgentDiscoveredPrinter>
{
    public void Configure(EntityTypeBuilder<PrintAgentDiscoveredPrinter> builder)
    {
        builder.ToTable("print_agent_discovered_printers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(260).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(260).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.PrintAgentId, x.NormalizedName }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.PrintAgentId, x.IsAvailable });
        builder.HasOne(x => x.PrintAgent).WithMany(x => x.DiscoveredPrinters)
            .HasForeignKey(x => x.PrintAgentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PrintAgentPairingCodeConfiguration : IEntityTypeConfiguration<PrintAgentPairingCode>
{
    public void Configure(EntityTypeBuilder<PrintAgentPairingCode> builder)
    {
        builder.ToTable("print_agent_pairing_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AgentName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.CodeHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.ExpiresAt, x.ConsumedAt });
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PrinterConfiguration : IEntityTypeConfiguration<Printer>
{
    public void Configure(EntityTypeBuilder<Printer> builder)
    {
        builder.ToTable("printers", table =>
        {
            table.HasCheckConstraint("CK_printers_PaperWidth", "\"PaperWidth\" IN (58, 80)");
            table.HasCheckConstraint("CK_printers_Port", "\"Port\" IS NULL OR (\"Port\" BETWEEN 1 AND 65535)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ConnectionType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LocalPrinterName).HasMaxLength(260);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.HasIndex(x => new { x.TenantId, x.PrintAgentId, x.Name }).IsUnique();
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PrintAgent).WithMany(x => x.Printers).HasForeignKey(x => x.PrintAgentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PrintingZoneConfiguration : IEntityTypeConfiguration<PrintingZone>
{
    public void Configure(EntityTypeBuilder<PrintingZone> builder)
    {
        builder.ToTable("printing_zones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.NormalizedName }).IsUnique();
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PrintAgent).WithMany(x => x.Zones).HasForeignKey(x => x.PrintAgentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PrintingZonePrinterConfiguration : IEntityTypeConfiguration<PrintingZonePrinter>
{
    public void Configure(EntityTypeBuilder<PrintingZonePrinter> builder)
    {
        builder.ToTable("printing_zone_printers");
        builder.HasKey(x => new { x.PrintingZoneId, x.PrinterId });
        builder.HasIndex(x => new { x.TenantId, x.PrintingZoneId });
        builder.HasOne(x => x.PrintingZone).WithMany(x => x.PrinterLinks).HasForeignKey(x => x.PrintingZoneId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Printer).WithMany(x => x.ZoneLinks).HasForeignKey(x => x.PrinterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CategoryPrintingRouteConfiguration : IEntityTypeConfiguration<CategoryPrintingRoute>
{
    public void Configure(EntityTypeBuilder<CategoryPrintingRoute> builder)
    {
        builder.ToTable("category_printing_routes");
        builder.HasKey(x => new { x.PrintingZoneId, x.CategoryId });
        builder.HasIndex(x => new { x.TenantId, x.CategoryId });
        builder.HasOne(x => x.PrintingZone).WithMany(x => x.CategoryRoutes).HasForeignKey(x => x.PrintingZoneId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductPrintingRouteConfiguration : IEntityTypeConfiguration<ProductPrintingRoute>
{
    public void Configure(EntityTypeBuilder<ProductPrintingRoute> builder)
    {
        builder.ToTable("product_printing_routes");
        builder.HasKey(x => new { x.PrintingZoneId, x.ProductId });
        builder.HasIndex(x => new { x.TenantId, x.ProductId });
        builder.HasOne(x => x.PrintingZone).WithMany(x => x.ProductRoutes).HasForeignKey(x => x.PrintingZoneId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PrintJobConfiguration : IEntityTypeConfiguration<PrintJob>
{
    public void Configure(EntityTypeBuilder<PrintJob> builder)
    {
        builder.ToTable("print_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SourceType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.PrintAgentId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceId });
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PrintAgent).WithMany().HasForeignKey(x => x.PrintAgentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Printer).WithMany().HasForeignKey(x => x.PrinterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PrintingZone).WithMany().HasForeignKey(x => x.PrintingZoneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OriginalPrintJob).WithMany().HasForeignKey(x => x.OriginalPrintJobId).OnDelete(DeleteBehavior.Restrict);
    }
}
