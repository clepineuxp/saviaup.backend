using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Images;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class UpdateCategoryUseCase(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IFileStorage? fileStorage = null,
    ITableRealtimeNotifier? realtime = null) : IUpdateCategoryUseCase
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
                request.Image,
                request.IsInventoryTracked,
                out var values))
        {
            return Result<CategoryDto>.Failure(Errors.Validation);
        }

        if (!ImageHelper.IsReferenceOwnedByTenant(values.Image, tenantId))
            return Result<CategoryDto>.Failure(Errors.Validation);

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
        var previousImagePath = category.ImagePath;

        if (!string.IsNullOrWhiteSpace(values.Image)
            && values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            if (fileStorage is null) return Result<CategoryDto>.Failure(Errors.Validation);
            try
            {
                var storedImage = await ImageHelper.SaveDataUrlAsync(
                    fileStorage, tenantId, "categories", categoryId.ToString("D"), values.Image, cancellationToken);
                if (storedImage is null) return Result<CategoryDto>.Failure(Errors.Validation);
                category.ImagePath = storedImage.Reference;
                category.ImageRef = null;
                category.ImageStored = null;
            }
            catch (InvalidDataException)
            {
                return Result<CategoryDto>.Failure(Errors.Validation);
            }
        }
        else if (string.IsNullOrWhiteSpace(values.Image))
        {
            category.ImagePath = null;
            category.ImageRef = null;
            category.ImageStored = null;
        }
        else if (!values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            category.ImagePath = values.Image;
            category.ImageRef = null;
            category.ImageStored = null;
        }

        category.Name = values.Name;
        category.NormalizedName = values.NormalizedName;
        category.Description = values.Description;
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
        if (fileStorage is not null
            && previousImagePath != category.ImagePath
            && ImageHelper.IsManagedReference(previousImagePath))
            await fileStorage.DeleteAsync(tenantId, previousImagePath, cancellationToken);
        if (realtime is not null)
            await realtime.SalesDataInvalidatedAsync(tenantId, new(["categories", "products"], now), cancellationToken);
        var usageCounts = await categoryRepository.GetUsageCountsAsync(tenantId, categoryId, cancellationToken);
        return Result<CategoryDto>.Success(CategoryRules.ToDto(category, usageCounts));
    }
}
