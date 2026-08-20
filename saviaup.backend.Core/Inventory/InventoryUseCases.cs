using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Inventory;

public sealed class ListInventoryUseCase(IIngredientRepository repository) : IListInventoryUseCase
{
    public async Task<Result<PagedResponse<InventoryItemDto>>> ExecuteAsync(
        Guid tenantId, InventoryQueryRequest request, CancellationToken cancellationToken)
    {
        var page = await repository.GetInventoryPageAsync(tenantId, request, cancellationToken);
        return Result<PagedResponse<InventoryItemDto>>.Success(InventoryRules.ToPage(page, request.Page, request.PageSize));
    }
}

public sealed class ListIngredientsUseCase(IIngredientRepository repository) : IListIngredientsUseCase
{
    public async Task<Result<PagedResponse<IngredientDto>>> ExecuteAsync(
        Guid tenantId, IngredientQueryRequest request, CancellationToken cancellationToken)
    {
        var page = await repository.GetPageAsync(tenantId, request, cancellationToken);
        var mapped = new PageData<IngredientDto>(page.Items.Select(InventoryRules.ToDto).ToArray(), page.TotalCount);
        return Result<PagedResponse<IngredientDto>>.Success(InventoryRules.ToPage(mapped, request.Page, request.PageSize));
    }
}

public sealed class CreateIngredientUseCase(
    IIngredientRepository ingredientRepository,
    IInventoryMovementRepository movementRepository,
    ICategoryRepository categoryRepository,
    IMeasurementUnitRepository unitRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICreateIngredientUseCase
{
    public async Task<Result<IngredientDto>> ExecuteAsync(
        Guid tenantId, Guid userId, CreateIngredientRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty || request.MeasurementUnitId == Guid.Empty
            || request.InitialStock is < 0 or > InventoryRules.MaximumStock
            || !InventoryRules.TryPrepareIngredient(request.Name, request.Description, request.MinimumStock, out var values))
            return Result<IngredientDto>.Failure(Errors.Validation);

        var category = await categoryRepository.GetByIdAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive) return Result<IngredientDto>.Failure(Errors.CategoryNotFound);
        var unit = await unitRepository.GetByIdAsync(tenantId, request.MeasurementUnitId, cancellationToken);
        if (unit is null || !unit.IsActive) return Result<IngredientDto>.Failure(Errors.MeasurementUnitNotFound);

        var now = clock.UtcNow;
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category.Id,
            Category = category,
            MeasurementUnitId = unit.Id,
            MeasurementUnit = unit,
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            Description = values.Description,
            MinimumStock = values.MinimumStock,
            CurrentStock = request.InitialStock,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await ingredientRepository.AddAsync(ingredient, cancellationToken);
        if (request.InitialStock > 0)
        {
            await movementRepository.AddAsync(new InventoryMovement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                IngredientId = ingredient.Id,
                Ingredient = ingredient,
                CreatedByUserId = userId,
                Direction = InventoryMovementCodes.Increase,
                Reason = InventoryMovementCodes.Initial,
                Quantity = request.InitialStock,
                StockBefore = 0,
                StockAfter = request.InitialStock,
                CreatedAt = now
            }, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<IngredientDto>.Success(InventoryRules.ToDto(ingredient));
    }
}

public sealed class UpdateIngredientUseCase(
    IIngredientRepository ingredientRepository,
    ICategoryRepository categoryRepository,
    IMeasurementUnitRepository unitRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IUpdateIngredientUseCase
{
    public async Task<Result<IngredientDto>> ExecuteAsync(
        Guid tenantId, Guid ingredientId, UpdateIngredientRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty || request.MeasurementUnitId == Guid.Empty
            || !InventoryRules.TryPrepareIngredient(request.Name, request.Description, request.MinimumStock, out var values))
            return Result<IngredientDto>.Failure(Errors.Validation);
        var ingredient = await ingredientRepository.GetByIdAsync(tenantId, ingredientId, cancellationToken);
        if (ingredient is null) return Result<IngredientDto>.Failure(Errors.IngredientNotFound);
        var category = await categoryRepository.GetByIdAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive) return Result<IngredientDto>.Failure(Errors.CategoryNotFound);
        var unit = await unitRepository.GetByIdAsync(tenantId, request.MeasurementUnitId, cancellationToken);
        if (unit is null || !unit.IsActive) return Result<IngredientDto>.Failure(Errors.MeasurementUnitNotFound);

        ingredient.CategoryId = category.Id; ingredient.Category = category;
        ingredient.MeasurementUnitId = unit.Id; ingredient.MeasurementUnit = unit;
        ingredient.Name = values.Name; ingredient.NormalizedName = values.NormalizedName;
        ingredient.Description = values.Description; ingredient.MinimumStock = values.MinimumStock;
        ingredient.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<IngredientDto>.Success(InventoryRules.ToDto(ingredient));
    }
}

public sealed class SetIngredientStatusUseCase(
    IIngredientRepository repository, IDateTimeProvider clock, IUnitOfWork unitOfWork) : ISetIngredientStatusUseCase
{
    public async Task<Result<IngredientDto>> ExecuteAsync(
        Guid tenantId, Guid ingredientId, SetIngredientStatusRequest request, CancellationToken cancellationToken)
    {
        var ingredient = await repository.GetByIdAsync(tenantId, ingredientId, cancellationToken);
        if (ingredient is null) return Result<IngredientDto>.Failure(Errors.IngredientNotFound);
        ingredient.IsActive = request.IsActive; ingredient.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<IngredientDto>.Success(InventoryRules.ToDto(ingredient));
    }
}

public sealed class DeleteIngredientUseCase(IIngredientRepository repository, IUnitOfWork unitOfWork) : IDeleteIngredientUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken)
    {
        var ingredient = await repository.GetByIdAsync(tenantId, ingredientId, cancellationToken);
        if (ingredient is null) return Result.Failure(Errors.IngredientNotFound);
        if (await repository.HasMovementsAsync(tenantId, ingredientId, cancellationToken))
            return Result.Failure(Errors.IngredientInUse);
        repository.Remove(ingredient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
