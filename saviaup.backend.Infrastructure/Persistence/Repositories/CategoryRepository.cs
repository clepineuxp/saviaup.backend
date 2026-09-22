using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyCollection<Category>> GetForTenantAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken,
        bool onlyWithProducts = false)
    {
        var query = context.Categories
            .AsNoTracking()
            .Include(category => category.ImageStored)
            .Where(category => category.TenantId == tenantId && (includeInactive || category.IsActive));

        if (onlyWithProducts)
        {
            query = query.Where(category => category.Products.Any(p => p.IsActive));
        }

        return await query
            .OrderBy(category => category.NormalizedName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Category?> GetByIdAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        => context.Categories
            .Include(category => category.ImageStored)
            .SingleOrDefaultAsync(
                category => category.TenantId == tenantId && category.Id == categoryId,
                cancellationToken);

    public Task<Category?> GetByIdAsNoTrackingAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
        => context.Categories
            .AsNoTracking()
            .Include(category => category.ImageStored)
            .SingleOrDefaultAsync(
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

    public async Task<IReadOnlyCollection<CategoryUsageCounts>> GetUsageCountsForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await UsageCountsQuery(tenantId).ToArrayAsync(cancellationToken);

    public Task<CategoryUsageCounts?> GetUsageCountsAsync(
        Guid tenantId,
        Guid categoryId,
        CancellationToken cancellationToken)
        => UsageCountsQuery(tenantId)
            .SingleOrDefaultAsync(counts => counts.CategoryId == categoryId, cancellationToken);

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

    private IQueryable<CategoryUsageCounts> UsageCountsQuery(Guid tenantId)
        => context.Categories
            .AsNoTracking()
            .Where(category => category.TenantId == tenantId)
            .Select(category => new CategoryUsageCounts(
                category.Id,
                category.Products.Count(),
                category.Products.SelectMany(product => product.Variations).Count(),
                category.Ingredients.Count()));
}
