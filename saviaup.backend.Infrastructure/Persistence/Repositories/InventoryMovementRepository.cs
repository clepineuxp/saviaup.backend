using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class InventoryMovementRepository(SaviaUpDbContext context) : IInventoryMovementRepository
{
    public async Task<PageData<InventoryMovement>> GetPageAsync(
        Guid tenantId, InventoryMovementQueryRequest request, CancellationToken cancellationToken)
    {
        var query = context.InventoryMovements.AsNoTracking()
            .Include(movement => movement.Ingredient).ThenInclude(ingredient => ingredient.MeasurementUnit)
            .Where(movement => movement.TenantId == tenantId);
        if (request.IngredientId.HasValue)
            query = query.Where(movement => movement.IngredientId == request.IngredientId.Value);
        if (!string.IsNullOrWhiteSpace(request.Direction))
        {
            var direction = request.Direction.Trim().ToLowerInvariant();
            query = query.Where(movement => movement.Direction == direction);
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PageData<InventoryMovement>(items, count);
    }

    public async Task AddAsync(InventoryMovement movement, CancellationToken cancellationToken)
        => await context.InventoryMovements.AddAsync(movement, cancellationToken);
}
