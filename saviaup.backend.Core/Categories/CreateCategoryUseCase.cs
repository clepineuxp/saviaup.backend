using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Images;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class CreateCategoryUseCase(
    ICategoryRepository categoryRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IFileStorage? fileStorage = null,
    ITableRealtimeNotifier? realtime = null) : ICreateCategoryUseCase
{
    public async Task<Result<CategoryDto>> ExecuteAsync(
        Guid tenantId,
        CreateCategoryRequest request,
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

        if (await categoryRepository.NameExistsAsync(
                tenantId,
                values.NormalizedName,
                null,
                cancellationToken))
        {
            return Result<CategoryDto>.Failure(Errors.CategoryNameAlreadyExists);
        }

        var now = dateTimeProvider.UtcNow;
        var categoryId = Guid.NewGuid();
        string? imagePath = null;

        if (!string.IsNullOrWhiteSpace(values.Image)
            && values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            if (fileStorage is null) return Result<CategoryDto>.Failure(Errors.Validation);
            try
            {
                var storedImage = await ImageHelper.SaveDataUrlAsync(
                    fileStorage, tenantId, "categories", categoryId.ToString("D"), values.Image, cancellationToken);
                if (storedImage is null) return Result<CategoryDto>.Failure(Errors.Validation);
                imagePath = storedImage.Reference;
            }
            catch (InvalidDataException)
            {
                return Result<CategoryDto>.Failure(Errors.Validation);
            }
        }
        else if (!string.IsNullOrWhiteSpace(values.Image)
            && !values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            imagePath = values.Image;

        var category = new Category
        {
            Id = categoryId,
            TenantId = tenantId,
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            Description = values.Description,
            ImagePath = imagePath,
            IsInventoryTracked = values.IsInventoryTracked,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (realtime is not null)
            await realtime.SalesDataInvalidatedAsync(tenantId, new(["categories", "products"], now), cancellationToken);
        return Result<CategoryDto>.Success(CategoryRules.ToDto(category));
    }
}
