using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform.Configurations;

public sealed class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.ToTable("tenant_invitations");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.Email).HasMaxLength(320).IsRequired();
        builder.Property(invitation => invitation.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.HasIndex(invitation => new { invitation.TenantId, invitation.NormalizedEmail }).IsUnique();
        builder.HasIndex(invitation => invitation.NormalizedEmail);
        builder.HasIndex(invitation => invitation.RoleId);
        builder.HasOne(invitation => invitation.Tenant).WithMany(tenant => tenant.Invitations)
            .HasForeignKey(invitation => invitation.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(invitation => invitation.InvitedByUser).WithMany()
            .HasForeignKey(invitation => invitation.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
