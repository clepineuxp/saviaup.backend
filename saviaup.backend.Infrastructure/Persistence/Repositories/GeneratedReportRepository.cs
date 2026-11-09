using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class GeneratedReportRepository(ApplicationDbContext context) : IGeneratedReportRepository
{
    public async Task<GeneratedReport?> GetByIdAsync(Guid tenantId, Guid reportId, CancellationToken cancellationToken)
        => await context.GeneratedReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == reportId, cancellationToken);

    public async Task<GeneratedReport?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await context.GeneratedReports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<GeneratedReport>> GetRecentAsync(Guid tenantId, int limit, CancellationToken cancellationToken)
        => await context.GeneratedReports
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

    public async Task AddAsync(GeneratedReport report, CancellationToken cancellationToken)
        => await context.GeneratedReports.AddAsync(report, cancellationToken);

    public void Remove(GeneratedReport report)
        => context.GeneratedReports.Remove(report);
}

