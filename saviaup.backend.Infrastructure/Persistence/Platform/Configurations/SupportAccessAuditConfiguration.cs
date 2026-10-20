using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform.Configurations;

public sealed class SupportAccessAuditConfiguration : IEntityTypeConfiguration<SupportAccessAudit>
{
    public void Configure(EntityTypeBuilder<SupportAccessAudit> builder)
    {
        builder.ToTable("support_access_audits");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Ticket).HasMaxLength(120);
        builder.HasIndex(audit => audit.SupportUserId);
        builder.HasIndex(audit => audit.TenantId);
        builder.HasIndex(audit => audit.AccessedAt);
        builder.HasOne(audit => audit.SupportUser).WithMany().HasForeignKey(audit => audit.SupportUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(audit => audit.Tenant).WithMany().HasForeignKey(audit => audit.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
