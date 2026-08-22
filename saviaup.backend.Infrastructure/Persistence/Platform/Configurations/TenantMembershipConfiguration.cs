using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform.Configurations;

public sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");
        builder.HasKey(membership => membership.Id);
        builder.HasIndex(membership => new { membership.UserId, membership.TenantId }).IsUnique();
        builder.HasIndex(membership => membership.UserId);
        builder.HasIndex(membership => membership.TenantId);
        builder.HasIndex(membership => membership.RoleId);
        builder.HasIndex(membership => membership.DisabledUntil);
        builder.HasOne(membership => membership.User).WithMany(user => user.Memberships).HasForeignKey(membership => membership.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(membership => membership.Tenant).WithMany(tenant => tenant.Memberships).HasForeignKey(membership => membership.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
