using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class CreateCategoryUseCase(
    ICategoryRepository categoryRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ICreateCategoryUseCase
{
    public async Task<Result<CategoryDto>> ExecuteAsync(
        Guid tenantId,
        CreateCategoryRequest request,
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

        if (await categoryRepository.NameExistsAsync(
                tenantId,
                values.NormalizedName,
                null,
                cancellationToken))
        {
            return Result<CategoryDto>.Failure(Errors.CategoryNameAlreadyExists);
        }

        var now = dateTimeProvider.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            Description = values.Description,
            ImageUrl = values.ImageUrl,
            IsInventoryTracked = values.IsInventoryTracked,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CategoryDto>.Success(CategoryRules.ToDto(category));
    }
}
