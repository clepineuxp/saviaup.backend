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
    IStoredImageRepository? imageRepository = null) : IUpdateCategoryUseCase
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

        if (imageRepository != null
            && !string.IsNullOrWhiteSpace(values.Image)
            && values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var storedImage = ImageHelper.CreateStoredImage(tenantId, "categories", categoryId.ToString(), values.Image, now);
            if (storedImage != null)
            {
                await imageRepository.AddAsync(storedImage, cancellationToken);
                category.ImageRef = storedImage.Id;
                category.ImageStored = storedImage;
            }
        }
        else if (string.IsNullOrWhiteSpace(values.Image))
        {
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
        return Result<CategoryDto>.Success(CategoryRules.ToDto(category));
    }
}
