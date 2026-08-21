using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class SetCategoryStatusUseCase(
    ICategoryRepository categoryRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ISetCategoryStatusUseCase
{
    public async Task<Result<CategoryDto>> ExecuteAsync(
        Guid tenantId,
        Guid categoryId,
        SetCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(tenantId, categoryId, cancellationToken);
        if (category is null) return Result<CategoryDto>.Failure(Errors.CategoryNotFound);

        category.IsActive = request.IsActive;
        category.UpdatedAt = dateTimeProvider.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CategoryDto>.Success(CategoryRules.ToDto(category));
    }
}
