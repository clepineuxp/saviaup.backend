using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SaviaUp.Backend.Infrastructure.Persistence;

public sealed class SaviaUpDbContextFactory : IDesignTimeDbContextFactory<SaviaUpDbContext>
{
    public SaviaUpDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<SaviaUpDbContextFactory>(optional: true)
            .Build();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SaviaUp")
            ?? configuration.GetConnectionString("SaviaUp")
            ?? throw new InvalidOperationException(
                "Configure ConnectionStrings:SaviaUp in .NET User Secrets or ConnectionStrings__SaviaUp in the environment.");

        var builder = new DbContextOptionsBuilder<SaviaUpDbContext>();
        builder.UseNpgsql(connectionString);
        return new SaviaUpDbContext(builder.Options);
    }
}
