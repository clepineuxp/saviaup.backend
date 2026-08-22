using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class IngredientRepository(ApplicationDbContext context) : IIngredientRepository
{
    public async Task<PageData<Ingredient>> GetPageAsync(
        Guid tenantId, IngredientQueryRequest request, CancellationToken cancellationToken)
    {
        var query = context.Ingredients.AsNoTracking()
            .Include(ingredient => ingredient.Category)
            .Include(ingredient => ingredient.MeasurementUnit)
            .Where(ingredient => ingredient.TenantId == tenantId
                && (request.IncludeInactive || ingredient.IsActive));
        if (request.CategoryId.HasValue)
            query = query.Where(ingredient => ingredient.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(ingredient => ingredient.NormalizedName.Contains(search));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(ingredient => ingredient.NormalizedName)
            .ThenBy(ingredient => ingredient.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PageData<Ingredient>(items, count);
    }

    public async Task<PageData<InventoryItemDto>> GetInventoryPageAsync(
        Guid tenantId, InventoryQueryRequest request, CancellationToken cancellationToken)
    {
        var query = context.Ingredients.AsNoTracking()
            .Include(ingredient => ingredient.Category)
            .Include(ingredient => ingredient.MeasurementUnit)
            .Where(ingredient => ingredient.TenantId == tenantId && ingredient.IsActive
                && ingredient.Category.IsActive && ingredient.Category.IsInventoryTracked);
        if (request.BelowMinimum.HasValue)
            query = request.BelowMinimum.Value
                ? query.Where(ingredient => ingredient.CurrentStock < ingredient.MinimumStock)
                : query.Where(ingredient => ingredient.CurrentStock >= ingredient.MinimumStock);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(ingredient => ingredient.NormalizedName.Contains(search));
        }
        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderBy(ingredient => ingredient.NormalizedName)
            .ThenBy(ingredient => ingredient.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        var items = entities.Select(ingredient => new InventoryItemDto(
            ingredient.Id, "ingredient", ingredient.Name,
            new CategoryReferenceDto(ingredient.Category.Id, ingredient.Category.Name, ingredient.Category.IsInventoryTracked),
            new MeasurementUnitDto(
                ingredient.MeasurementUnit.Id, ingredient.MeasurementUnit.Code, ingredient.MeasurementUnit.Name,
                ingredient.MeasurementUnit.IsActive, ingredient.MeasurementUnit.CreatedAt, ingredient.MeasurementUnit.UpdatedAt),
            ingredient.CurrentStock, ingredient.MinimumStock, ingredient.CurrentStock < ingredient.MinimumStock)).ToArray();
        return new PageData<InventoryItemDto>(items, count);
    }

    public Task<Ingredient?> GetByIdAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken)
        => context.Ingredients.Include(ingredient => ingredient.Category)
            .Include(ingredient => ingredient.MeasurementUnit)
            .SingleOrDefaultAsync(ingredient => ingredient.TenantId == tenantId && ingredient.Id == ingredientId, cancellationToken);

    public async Task<Ingredient?> GetForStockUpdateAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken)
    {
        if (context.Database.IsRelational())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM ingredients WHERE \"TenantId\" = {tenantId} AND \"Id\" = {ingredientId} FOR UPDATE",
                cancellationToken);
        }
        return await GetByIdAsync(tenantId, ingredientId, cancellationToken);
    }

    public async Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken)
        => await context.Ingredients.AddAsync(ingredient, cancellationToken);

    public Task<bool> HasMovementsAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken)
        => context.InventoryMovements.AsNoTracking().AnyAsync(
            movement => movement.TenantId == tenantId && movement.IngredientId == ingredientId, cancellationToken);

    public void Remove(Ingredient ingredient) => context.Ingredients.Remove(ingredient);
}
