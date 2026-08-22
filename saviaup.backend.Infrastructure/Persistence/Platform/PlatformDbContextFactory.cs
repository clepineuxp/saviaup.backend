using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SaviaUp.Backend.Infrastructure.Persistence.Platform;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.Combine(basePath, "..", "saviaup.backend.Api");
        var targetDirectory = Directory.Exists(apiPath) && File.Exists(Path.Combine(apiPath, "appsettings.json")) ? apiPath : basePath;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(targetDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<PlatformDbContextFactory>(optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("PlatformDatabase")
            ?? throw new InvalidOperationException("Connection string for PlatformDatabase is required.");

        var builder = new DbContextOptionsBuilder<PlatformDbContext>();
        builder.UseNpgsql(connectionString);
        return new PlatformDbContext(builder.Options);
    }
}
