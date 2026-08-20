using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class ListCategoriesUseCase(ICategoryRepository categoryRepository) : IListCategoriesUseCase
{
    public async Task<Result<IReadOnlyCollection<CategoryDto>>> ExecuteAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetForTenantAsync(tenantId, includeInactive, cancellationToken);
        return Result<IReadOnlyCollection<CategoryDto>>.Success(categories.Select(CategoryRules.ToDto).ToArray());
    }
}
