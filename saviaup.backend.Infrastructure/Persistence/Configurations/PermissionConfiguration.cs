using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Code).HasMaxLength(120).IsRequired();
        builder.Property(permission => permission.Description).HasMaxLength(500).IsRequired();
        builder.HasIndex(permission => permission.Code).IsUnique();
        builder.HasIndex(permission => permission.ModuleId);
        builder.HasOne(permission => permission.Module).WithMany(module => module.Permissions).HasForeignKey(permission => permission.ModuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasData(SeedData.Permissions);
    }
}
