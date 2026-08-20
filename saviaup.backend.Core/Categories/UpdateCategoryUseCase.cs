using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class UpdateCategoryUseCase(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : IUpdateCategoryUseCase
{
    public async Task<Result<CategoryDto>> ExecuteAsync(
        Guid tenantId,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!CategoryRules.TryPrepare(
                request.Name,
                request.Description,
                request.ImageUrl,
                request.IsInventoryTracked,
                out var values))
        {
            return Result<CategoryDto>.Failure(Errors.Validation);
        }

        var category = await categoryRepository.GetByIdAsync(tenantId, categoryId, cancellationToken);
        if (category is null) return Result<CategoryDto>.Failure(Errors.CategoryNotFound);
        if (await categoryRepository.NameExistsAsync(
                tenantId,
                values.NormalizedName,
                categoryId,
                cancellationToken))
        {
            return Result<CategoryDto>.Failure(Errors.CategoryNameAlreadyExists);
        }

        var now = dateTimeProvider.UtcNow;
        var inventoryTrackingWasDisabled = category.IsInventoryTracked && !values.IsInventoryTracked;
        category.Name = values.Name;
        category.NormalizedName = values.NormalizedName;
        category.Description = values.Description;
        category.ImageUrl = values.ImageUrl;
        category.IsInventoryTracked = values.IsInventoryTracked;
        category.UpdatedAt = now;
        if (inventoryTrackingWasDisabled)
        {
            await productRepository.DisableInventoryTrackingByCategoryAsync(
                tenantId,
                categoryId,
                now,
                cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CategoryDto>.Success(CategoryRules.ToDto(category));
    }
}
