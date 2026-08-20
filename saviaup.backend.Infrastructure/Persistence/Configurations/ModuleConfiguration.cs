using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

public sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("modules");
        builder.HasKey(module => module.Id);
        builder.Property(module => module.Code).HasMaxLength(80).IsRequired();
        builder.Property(module => module.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(module => module.Code).IsUnique();
        builder.HasData(SeedData.Modules);
    }
}
