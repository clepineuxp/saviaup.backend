using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

internal sealed class GeneratedReportConfiguration : IEntityTypeConfiguration<GeneratedReport>
{
    public void Configure(EntityTypeBuilder<GeneratedReport> builder)
    {
        builder.ToTable("generated_reports");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ReportType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FromDate).IsRequired();
        builder.Property(x => x.ToDate).IsRequired();
        builder.Property(x => x.ParametersJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.FileReference).HasMaxLength(500);
        builder.Property(x => x.FileSize);
        builder.Property(x => x.ErrorMessage).HasColumnType("text");
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedByUserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CompletedAt);

        builder.HasIndex(x => new { x.TenantId, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.ReportType });
    }
}

