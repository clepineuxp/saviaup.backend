using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
        builder.HasIndex(rolePermission => rolePermission.PermissionId);
        builder.HasOne(rolePermission => rolePermission.Role).WithMany(role => role.RolePermissions).HasForeignKey(rolePermission => rolePermission.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
