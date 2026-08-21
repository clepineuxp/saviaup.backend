using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class RestaurantTableRepository(SaviaUpDbContext context) : IRestaurantTableRepository
{
    public async Task<IReadOnlyCollection<RestaurantTable>> GetForTenantAsync(
        Guid tenantId,
        Guid? areaId,
        CancellationToken cancellationToken)
    {
        var query = context.RestaurantTables.AsNoTracking().Where(table => table.TenantId == tenantId);
        if (areaId.HasValue) query = query.Where(table => table.DiningAreaId == areaId.Value);
        return await query.OrderBy(table => table.DiningArea.Order)
            .ThenBy(table => table.NormalizedName)
            .ThenBy(table => table.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DiningArea>> GetOperationAreasAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await context.DiningAreas.AsNoTracking()
            .Where(area => area.TenantId == tenantId && area.IsActive)
            .Include(area => area.Tables)
            .OrderBy(area => area.Order)
            .ThenBy(area => area.Id)
            .ToArrayAsync(cancellationToken);

    public Task<RestaurantTable?> GetByIdAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken)
        => context.RestaurantTables.SingleOrDefaultAsync(
            table => table.TenantId == tenantId && table.Id == tableId,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid tenantId,
        Guid areaId,
        string normalizedName,
        Guid? excludedTableId,
        CancellationToken cancellationToken)
        => context.RestaurantTables.AnyAsync(
            table => table.TenantId == tenantId
                && table.DiningAreaId == areaId
                && table.NormalizedName == normalizedName
                && (!excludedTableId.HasValue || table.Id != excludedTableId.Value),
            cancellationToken);

    public async Task AddAsync(RestaurantTable table, CancellationToken cancellationToken)
        => await context.RestaurantTables.AddAsync(table, cancellationToken);

    public void Remove(RestaurantTable table) => context.RestaurantTables.Remove(table);
}
