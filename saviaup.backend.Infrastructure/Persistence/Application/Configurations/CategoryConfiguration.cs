using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(120).IsRequired();
        builder.Property(category => category.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.Property(category => category.ImageRef);
        builder.HasOne(category => category.ImageStored)
            .WithMany()
            .HasForeignKey(category => category.ImageRef)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(category => new { category.TenantId, category.NormalizedName }).IsUnique();
        builder.HasIndex(category => new { category.TenantId, category.IsActive, category.Name });
    }
}
