using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class DiningAreaRepository(SaviaUpDbContext context) : IDiningAreaRepository
{
    public async Task<IReadOnlyCollection<DiningArea>> GetForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await context.DiningAreas.AsNoTracking()
            .Where(area => area.TenantId == tenantId)
            .OrderBy(area => area.Order)
            .ThenBy(area => area.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<DiningArea>> GetForUpdateAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await context.DiningAreas
            .Where(area => area.TenantId == tenantId)
            .OrderBy(area => area.Order)
            .ThenBy(area => area.Id)
            .ToArrayAsync(cancellationToken);

    public Task<DiningArea?> GetByIdAsync(Guid tenantId, Guid areaId, CancellationToken cancellationToken)
        => context.DiningAreas.SingleOrDefaultAsync(
            area => area.TenantId == tenantId && area.Id == areaId,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludedAreaId,
        CancellationToken cancellationToken)
        => context.DiningAreas.AnyAsync(
            area => area.TenantId == tenantId
                && area.NormalizedName == normalizedName
                && (!excludedAreaId.HasValue || area.Id != excludedAreaId.Value),
            cancellationToken);

    public Task<bool> OrderExistsAsync(
        Guid tenantId,
        int order,
        Guid? excludedAreaId,
        CancellationToken cancellationToken)
        => context.DiningAreas.AnyAsync(
            area => area.TenantId == tenantId
                && area.Order == order
                && (!excludedAreaId.HasValue || area.Id != excludedAreaId.Value),
            cancellationToken);

    public async Task<int> GetNextOrderAsync(Guid tenantId, CancellationToken cancellationToken)
        => (await context.DiningAreas
            .Where(area => area.TenantId == tenantId)
            .Select(area => (int?)area.Order)
            .MaxAsync(cancellationToken) ?? 0) + 1;

    public async Task AddAsync(DiningArea area, CancellationToken cancellationToken)
        => await context.DiningAreas.AddAsync(area, cancellationToken);

    public Task<bool> IsInUseAsync(Guid tenantId, Guid areaId, CancellationToken cancellationToken)
        => context.RestaurantTables.AnyAsync(
            table => table.TenantId == tenantId && table.DiningAreaId == areaId,
            cancellationToken);

    public void Remove(DiningArea area) => context.DiningAreas.Remove(area);
}
