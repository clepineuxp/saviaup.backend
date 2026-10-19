using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform.Configurations;

public sealed class OrganizationAccessLinkConfiguration : IEntityTypeConfiguration<OrganizationAccessLink>
{
    public void Configure(EntityTypeBuilder<OrganizationAccessLink> builder)
    {
        builder.ToTable("organization_access_links");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.CodeHash).HasMaxLength(64).IsRequired();
        builder.Property(link => link.ProtectedCode).HasMaxLength(2048);
        builder.Property(link => link.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(link => link.CodeHash).IsUnique();
        builder.HasIndex(link => new { link.TenantId, link.ExpiresAt, link.AcceptedAt });
        builder.HasOne(link => link.Tenant).WithMany().HasForeignKey(link => link.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(link => link.RequestedByUser).WithMany().HasForeignKey(link => link.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(link => link.CreatedByUser).WithMany().HasForeignKey(link => link.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
