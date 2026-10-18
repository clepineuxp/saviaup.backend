using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class PromotionTargetConfiguration : IEntityTypeConfiguration<PromotionTarget>
{
    public void Configure(EntityTypeBuilder<PromotionTarget> builder)
    {
        builder.ToTable("promotion_targets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TargetType)
            .HasConversion(value => value.ToString().ToUpperInvariant(), value => Enum.Parse<PromotionTargetType>(value, true))
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.PromotionId });
        builder.HasIndex(x => new { x.TenantId, x.ProductId });
        builder.HasIndex(x => new { x.TenantId, x.ProductVariationId });
        builder.HasIndex(x => new { x.TenantId, x.CategoryId });
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductVariation).WithMany().HasForeignKey(x => x.ProductVariationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
