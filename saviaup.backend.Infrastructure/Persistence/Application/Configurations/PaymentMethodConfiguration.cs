using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");
        builder.HasKey(method => method.Id);
        builder.Property(method => method.Name).HasMaxLength(120).IsRequired();
        builder.Property(method => method.NormalizedName).HasMaxLength(120).IsRequired();
        builder.HasIndex(method => new { method.TenantId, method.NormalizedName }).IsUnique();
        builder.HasIndex(method => new { method.TenantId, method.IsActive });
    }
}
