using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class DeleteCategoryUseCase(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : IDeleteCategoryUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(tenantId, categoryId, cancellationToken);
        if (category is null) return Result.Failure(Errors.CategoryNotFound);
        if (await categoryRepository.IsInUseAsync(tenantId, categoryId, cancellationToken))
            return Result.Failure(Errors.CategoryInUse);

        categoryRepository.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
