using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Inventory;

public sealed class ListInventoryMovementsUseCase(IInventoryMovementRepository repository) : IListInventoryMovementsUseCase
{
    public async Task<Result<PagedResponse<InventoryMovementDto>>> ExecuteAsync(
        Guid tenantId, InventoryMovementQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.Direction is not null && !IsDirection(request.Direction.Trim()))
            return Result<PagedResponse<InventoryMovementDto>>.Failure(Errors.InventoryMovementInvalid);
        var page = await repository.GetPageAsync(tenantId, request, cancellationToken);
        var mapped = new PageData<InventoryMovementDto>(page.Items.Select(InventoryRules.ToDto).ToArray(), page.TotalCount);
        return Result<PagedResponse<InventoryMovementDto>>.Success(InventoryRules.ToPage(mapped, request.Page, request.PageSize));
    }

    private static bool IsDirection(string value)
        => value.Equals(InventoryMovementCodes.Increase, StringComparison.OrdinalIgnoreCase)
            || value.Equals(InventoryMovementCodes.Decrease, StringComparison.OrdinalIgnoreCase);
}

public sealed class CreateInventoryMovementUseCase(
    IIngredientRepository ingredientRepository,
    IInventoryMovementRepository movementRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICreateInventoryMovementUseCase
{
    public Task<Result<InventoryMovementDto>> ExecuteAsync(
        Guid tenantId, Guid userId, CreateInventoryMovementRequest request, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var direction = request.Direction?.Trim().ToLowerInvariant();
            var reason = request.Reason?.Trim().ToLowerInvariant();
            if (request.IngredientId == Guid.Empty
                || request.Quantity is <= 0 or > InventoryRules.MaximumStock
                || !IsValid(direction, reason))
                return Result<InventoryMovementDto>.Failure(Errors.InventoryMovementInvalid);

            var ingredient = await ingredientRepository.GetForStockUpdateAsync(tenantId, request.IngredientId, transactionToken);
            if (ingredient is null || !ingredient.IsActive)
                return Result<InventoryMovementDto>.Failure(Errors.IngredientNotFound);
            if (direction == InventoryMovementCodes.Decrease && ingredient.CurrentStock < request.Quantity)
                return Result<InventoryMovementDto>.Failure(Errors.InventoryInsufficientStock);

            var before = ingredient.CurrentStock;
            var after = direction == InventoryMovementCodes.Increase ? before + request.Quantity : before - request.Quantity;
            if (after > InventoryRules.MaximumStock)
                return Result<InventoryMovementDto>.Failure(Errors.InventoryMovementInvalid);
            var now = clock.UtcNow;
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                IngredientId = ingredient.Id,
                Ingredient = ingredient,
                CreatedByUserId = userId,
                Direction = direction!,
                Reason = reason!,
                Quantity = request.Quantity,
                StockBefore = before,
                StockAfter = after,
                Note = InventoryRules.CleanOptional(request.Note),
                CreatedAt = now
            };
            ingredient.CurrentStock = after;
            ingredient.UpdatedAt = now;
            await movementRepository.AddAsync(movement, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            return Result<InventoryMovementDto>.Success(InventoryRules.ToDto(movement));
        }, cancellationToken);

    private static bool IsValid(string? direction, string? reason)
        => direction switch
        {
            InventoryMovementCodes.Increase => reason is InventoryMovementCodes.Purchase
                or InventoryMovementCodes.Production or InventoryMovementCodes.Acquisition,
            InventoryMovementCodes.Decrease => reason is InventoryMovementCodes.Expiration
                or InventoryMovementCodes.Loss or InventoryMovementCodes.Waste,
            _ => false
        };
}
