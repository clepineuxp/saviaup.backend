using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class OrganizationParameterConfiguration : IEntityTypeConfiguration<OrganizationParameter>
{
    public void Configure(EntityTypeBuilder<OrganizationParameter> builder)
    {
        builder.ToTable("organization_parameters");
        builder.HasKey(parameter => parameter.Id);
        builder.Property(parameter => parameter.Key).HasMaxLength(120).IsRequired();
        builder.Property(parameter => parameter.Value).HasMaxLength(2000).IsRequired();
        builder.Property(parameter => parameter.ValueType).HasMaxLength(20).IsRequired();
        builder.HasIndex(parameter => new { parameter.TenantId, parameter.Key }).IsUnique();
        builder.HasOne(parameter => parameter.Tenant).WithMany(tenant => tenant.Parameters)
            .HasForeignKey(parameter => parameter.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
