using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class PromotionRepository(ApplicationDbContext context) : IPromotionRepository
{
    public async Task<IReadOnlyCollection<Promotion>> GetForTenantAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken)
        => await Query(tenantId)
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Promotion>> GetActiveForEvaluationAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await Query(tenantId)
            .Where(x => x.IsActive)
            .ToArrayAsync(cancellationToken);

    public Task<Promotion?> GetByIdAsync(Guid tenantId, Guid promotionId, CancellationToken cancellationToken)
        => Query(tenantId).SingleOrDefaultAsync(x => x.Id == promotionId, cancellationToken);

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
        => await context.Promotions.AddAsync(promotion, cancellationToken);

    public void Remove(Promotion promotion) => context.Promotions.Remove(promotion);

    private IQueryable<Promotion> Query(Guid tenantId)
        => context.Promotions
            .IgnoreQueryFilters()
            .Include(x => x.Targets).ThenInclude(x => x.Product)
            .Include(x => x.Targets).ThenInclude(x => x.ProductVariation)
            .Include(x => x.Targets).ThenInclude(x => x.Category)
            .Where(x => x.TenantId == tenantId);
}
