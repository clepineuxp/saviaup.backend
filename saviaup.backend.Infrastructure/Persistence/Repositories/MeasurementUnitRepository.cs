using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class MeasurementUnitRepository(SaviaUpDbContext context) : IMeasurementUnitRepository
{
    public async Task<PageData<MeasurementUnit>> GetPageAsync(
        Guid tenantId, MeasurementUnitQueryRequest request, CancellationToken cancellationToken)
    {
        var query = context.MeasurementUnits.AsNoTracking()
            .Where(unit => unit.TenantId == tenantId && (request.IncludeInactive || unit.IsActive));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(unit => unit.NormalizedCode.Contains(search) || unit.NormalizedName.Contains(search));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(unit => unit.NormalizedName).ThenBy(unit => unit.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PageData<MeasurementUnit>(items, count);
    }

    public Task<MeasurementUnit?> GetByIdAsync(Guid tenantId, Guid unitId, CancellationToken cancellationToken)
        => context.MeasurementUnits.SingleOrDefaultAsync(
            unit => unit.TenantId == tenantId && unit.Id == unitId, cancellationToken);

    public Task<bool> CodeOrNameExistsAsync(
        Guid tenantId, string normalizedCode, string normalizedName, Guid? excludedUnitId, CancellationToken cancellationToken)
        => context.MeasurementUnits.AsNoTracking().AnyAsync(unit => unit.TenantId == tenantId
            && (unit.NormalizedCode == normalizedCode || unit.NormalizedName == normalizedName)
            && (!excludedUnitId.HasValue || unit.Id != excludedUnitId.Value), cancellationToken);

    public async Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken)
        => await context.MeasurementUnits.AddAsync(unit, cancellationToken);

    public async Task AddDefaultsAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        MeasurementUnit[] units =
        [
            Create(tenantId, "gr", "gramos", now),
            Create(tenantId, "kg", "kilogramos", now),
            Create(tenantId, "und", "unidades", now)
        ];
        await context.MeasurementUnits.AddRangeAsync(units, cancellationToken);
    }

    public Task<bool> IsInUseAsync(Guid tenantId, Guid unitId, CancellationToken cancellationToken)
        => context.Ingredients.AsNoTracking().AnyAsync(
            ingredient => ingredient.TenantId == tenantId && ingredient.MeasurementUnitId == unitId, cancellationToken);

    public void Remove(MeasurementUnit unit) => context.MeasurementUnits.Remove(unit);

    private static MeasurementUnit Create(Guid tenantId, string code, string name, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Code = code,
        NormalizedCode = code.ToUpperInvariant(),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };
}
