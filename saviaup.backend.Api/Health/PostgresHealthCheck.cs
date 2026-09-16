using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Api.Health;

public sealed class PostgresHealthCheck(
    PlatformDbContext platformContext,
    ApplicationDbContext appContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var platformCanConnect = await platformContext.Database.CanConnectAsync(cancellationToken);
        var appCanConnect = await appContext.Database.CanConnectAsync(cancellationToken);

        if (platformCanConnect && appCanConnect)
            return HealthCheckResult.Healthy("PostgreSQL databases (saviaup_platform and saviaup_app) are reachable.");

        return HealthCheckResult.Unhealthy($"PostgreSQL connection state: Platform={platformCanConnect}, App={appCanConnect}");
    }
}
