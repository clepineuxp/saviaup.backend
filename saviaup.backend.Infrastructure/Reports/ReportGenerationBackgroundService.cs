using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Reports;

public sealed class ReportGenerationBackgroundService(
    IReportJobQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ReportGenerationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Report generation background service started.");

        try
        {
            using var initScope = scopeFactory.CreateScope();
            var dbContext = initScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var interruptedReports = await dbContext.GeneratedReports
                .IgnoreQueryFilters()
                .Where(r => r.Status == "PENDING" || r.Status == "PROCESSING")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(stoppingToken);

            foreach (var rep in interruptedReports)
            {
                if (rep.Status == "PROCESSING")
                {
                    rep.Status = "PENDING";
                }
                await queue.EnqueueAsync(rep.Id, stoppingToken);
            }

            if (interruptedReports.Count > 0)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
                logger.LogInformation("Re-enqueued {Count} pending/interrupted reports on startup.", interruptedReports.Count);
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not re-enqueue pending reports on startup.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var reportId = await queue.DequeueAsync(stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var reportUseCase = scope.ServiceProvider.GetRequiredService<IReportsUseCase>();

                await reportUseCase.ProcessReportJobAsync(reportId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing report generation job.");
            }
        }

        logger.LogInformation("Report generation background service stopped.");
    }
}

