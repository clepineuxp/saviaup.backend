using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Application;

public sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Empty;
    public bool HasTenant => false;
}

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.Combine(basePath, "..", "saviaup.backend.Api");
        var targetDirectory = Directory.Exists(apiPath) && File.Exists(Path.Combine(apiPath, "appsettings.json")) ? apiPath : basePath;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(targetDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("ApplicationDatabase")
            ?? throw new InvalidOperationException("Connection string for ApplicationDatabase is required.");

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseNpgsql(connectionString);
        return new ApplicationDbContext(builder.Options, new DesignTimeTenantContext());
    }
}
