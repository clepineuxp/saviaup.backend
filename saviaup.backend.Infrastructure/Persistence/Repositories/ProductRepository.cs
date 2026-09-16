using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public async Task<PageData<Product>> GetPageAsync(
        Guid tenantId,
        ProductQueryRequest request,
        ProductType? type,
        CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.ImageStored)
            .Include(product => product.RecipeItems)
                .ThenInclude(item => item.Ingredient)
                    .ThenInclude(ingredient => ingredient!.MeasurementUnit)
            .Where(product => product.TenantId == tenantId
                && (request.IncludeInactive || product.IsActive));
        if (request.CategoryId.HasValue)
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        if (type.HasValue)
            query = query.Where(product => product.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(product => product.NormalizedName.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(product => product.NormalizedName)
            .ThenBy(product => product.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PageData<Product>(items, totalCount);
    }

    public Task<Product?> GetByIdAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
        => context.Products.Include(product => product.Category)
            .Include(product => product.ImageStored)
            .Include(product => product.RecipeItems)
                .ThenInclude(item => item.Ingredient)
                    .ThenInclude(ingredient => ingredient!.MeasurementUnit)
            .SingleOrDefaultAsync(
                product => product.TenantId == tenantId && product.Id == productId,
                cancellationToken);

    /// <summary>
    /// Carga el producto SIN RecipeItems en el ChangeTracker.
    /// Usar exclusivamente en el path de actualización para evitar conflictos
    /// de concurrencia cuando se gestionan recetas con ExecuteDeleteAsync.
    /// </summary>
    public Task<Product?> GetByIdForUpdateAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
        => context.Products
            .Include(product => product.Category)
            .Include(product => product.ImageStored)
            .SingleOrDefaultAsync(
                product => product.TenantId == tenantId && product.Id == productId,
                cancellationToken);

    public async Task<IReadOnlyCollection<Product>> GetByIdsWithRecipesAsync(
        Guid tenantId,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var idList = productIds.Distinct().ToList();
        if (idList.Count == 0) return [];

        return await context.Products.AsNoTracking()
            .Include(product => product.RecipeItems)
                .ThenInclude(item => item.Ingredient)
            .Where(product => product.TenantId == tenantId && idList.Contains(product.Id))
            .ToArrayAsync(cancellationToken);
    }

    public async Task DisableInventoryTrackingByCategoryAsync(
        Guid tenantId,
        Guid categoryId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var products = await context.Products
            .Where(product => product.TenantId == tenantId
                && product.CategoryId == categoryId
                && product.IsInventoryTracked)
            .ToArrayAsync(cancellationToken);
        foreach (var product in products)
        {
            product.IsInventoryTracked = false;
            product.UpdatedAt = updatedAt;
        }
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
        => await context.Products.AddAsync(product, cancellationToken);

    /// <summary>
    /// Elimina todos los recipe items de un producto directamente en base de datos,
    /// sin pasar por el Change Tracker de EF Core. Esto evita DbUpdateConcurrencyException
    /// cuando los RecipeItems están rastreados en el contexto actual.
    /// </summary>
    public Task DeleteRecipeItemsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
        => context.ProductRecipeItems
            .Where(item => item.TenantId == tenantId && item.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task AddRecipeItemsAsync(IEnumerable<ProductRecipeItem> items, CancellationToken cancellationToken)
        => await context.ProductRecipeItems.AddRangeAsync(items, cancellationToken);

    public void Remove(Product product) => context.Products.Remove(product);
}
