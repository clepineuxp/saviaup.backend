using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SaviaUp.Backend.Infrastructure.Persistence;
using SaviaUp.Backend.Infrastructure.Email;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class SaviaUpApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"saviaup-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
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
            services.RemoveAll<DbContextOptions<SaviaUpDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SaviaUpDbContext>>();
            services.RemoveAll<SaviaUpDbContext>();
            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender, DevelopmentEmailSender>();
            var inMemoryServices = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<SaviaUpDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .UseInternalServiceProvider(inMemoryServices));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SaviaUpDbContext>().Database.EnsureCreated();
        return host;
    }
}
