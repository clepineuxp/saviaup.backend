using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyCollection<Category>> GetForTenantAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken)
        => await context.Categories
            .AsNoTracking()
            .Where(category => category.TenantId == tenantId && (includeInactive || category.IsActive))
            .OrderBy(category => category.NormalizedName)
            .ToArrayAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        => context.Categories.SingleOrDefaultAsync(
            category => category.TenantId == tenantId && category.Id == categoryId,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludedCategoryId,
        CancellationToken cancellationToken)
        => context.Categories.AsNoTracking().AnyAsync(
            category => category.TenantId == tenantId
                && category.NormalizedName == normalizedName
                && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value),
            cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
        => await context.Categories.AddAsync(category, cancellationToken);

    public async Task<bool> IsInUseAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        => await context.Ingredients.AsNoTracking().AnyAsync(
                ingredient => ingredient.TenantId == tenantId && ingredient.CategoryId == categoryId,
                cancellationToken)
            || await context.Products.AsNoTracking().AnyAsync(
                product => product.TenantId == tenantId && product.CategoryId == categoryId,
                cancellationToken);

    public void Remove(Category category) => context.Categories.Remove(category);
}
