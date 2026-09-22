using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Categories;

public sealed class ListCategoriesUseCase(ICategoryRepository categoryRepository) : IListCategoriesUseCase
{
    public async Task<Result<IReadOnlyCollection<CategoryDto>>> ExecuteAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken,
        bool onlyWithProducts = false)
    {
        var categories = await categoryRepository.GetForTenantAsync(tenantId, includeInactive, cancellationToken, onlyWithProducts);
        var usageByCategoryId = (await categoryRepository.GetUsageCountsForTenantAsync(tenantId, cancellationToken))
            .ToDictionary(counts => counts.CategoryId);
        return Result<IReadOnlyCollection<CategoryDto>>.Success(categories
            .Select(category => CategoryRules.ToDto(
                category,
                usageByCategoryId.GetValueOrDefault(category.Id)))
            .ToArray());
    }
}
