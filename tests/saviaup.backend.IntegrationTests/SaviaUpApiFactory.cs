using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Email;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class SaviaUpApiFactory : WebApplicationFactory<Program>
{
    private readonly string _platformDbName = $"saviaup-platform-tests-{Guid.NewGuid()}";
    private readonly string _appDbName = $"saviaup-app-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = "Host=localhost;Database=unused_platform",
                ["ConnectionStrings:ApplicationDatabase"] = "Host=localhost;Database=unused_app",
                ["ConnectionStrings:SaviaUp"] = "Host=localhost;Database=unused",
                ["Jwt:Issuer"] = "saviaup-tests",
                ["Jwt:Audience"] = "saviaup-test-client",
                ["Jwt:SigningKey"] = "integration-tests-only-signing-key-with-more-than-32-bytes",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30",
                ["Email:Mode"] = "Development",
                ["Frontend:BaseUrl"] = "http://localhost:4200"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PlatformDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PlatformDbContext>>();
            services.RemoveAll<PlatformDbContext>();

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender, DevelopmentEmailSender>();

            var inMemoryServices = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<PlatformDbContext>(options => options
                .UseInMemoryDatabase(_platformDbName)
                .UseInternalServiceProvider(inMemoryServices));

            services.AddDbContext<ApplicationDbContext>(options => options
                .UseInMemoryDatabase(_appDbName)
                .UseInternalServiceProvider(inMemoryServices));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        return host;
    }
}
