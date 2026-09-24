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
            .AsSplitQuery()
            .Include(product => product.Category)
            .Include(product => product.ImageStored)
            .Include(product => product.RecipeItems)
                .ThenInclude(item => item.Ingredient)
                    .ThenInclude(ingredient => ingredient!.MeasurementUnit)
            .Include(product => product.Variations)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.Product)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.ProductVariation)
            .Where(product => product.TenantId == tenantId
                && (request.IncludeInactive || product.IsActive));
        if (request.CategoryId.HasValue)
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        if (type.HasValue)
            query = query.Where(product => product.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(product => product.NormalizedName.Contains(search)
                || product.Variations.Any(v => v.IsActive && v.NormalizedName.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(product => product.NormalizedName)
            .ThenBy(product => product.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PageData<Product>(items, totalCount);
    }

    public async Task<IReadOnlyCollection<Product>> GetAllForTenantAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken)
        => await context.Products.AsNoTracking()
            .Include(product => product.Variations)
            .Where(product => product.TenantId == tenantId && (includeInactive || product.IsActive))
            .OrderBy(product => product.NormalizedName)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Product>> GetSalesCatalogAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
        => await context.Products.AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.Category)
            .Include(product => product.ImageStored)
            .Include(product => product.RecipeItems)
                .ThenInclude(item => item.Ingredient)
                    .ThenInclude(ingredient => ingredient!.MeasurementUnit)
            .Include(product => product.Variations)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.Product)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.ProductVariation)
            .Where(product => product.TenantId == tenantId
                && product.IsActive
                && product.Category.IsActive)
            .OrderBy(product => product.NormalizedName)
            .ThenBy(product => product.Id)
            .ToArrayAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
        => context.Products.AsSplitQuery()
            .Include(product => product.Category)
            .Include(product => product.ImageStored)
            .Include(product => product.RecipeItems)
                .ThenInclude(item => item.Ingredient)
                    .ThenInclude(ingredient => ingredient!.MeasurementUnit)
            .Include(product => product.Variations)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.Product)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.ProductVariation)
            .SingleOrDefaultAsync(
                product => product.TenantId == tenantId && product.Id == productId,
                cancellationToken);

    /// <summary>
    /// Carga el producto SIN RecipeItems ni Variations en el ChangeTracker.
    /// Usar exclusivamente en el path de actualización para evitar conflictos
    /// de concurrencia cuando se gestionan recetas y variaciones con ExecuteDeleteAsync.
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
            .AsSplitQuery()
            .Include(product => product.RecipeItems)
                .ThenInclude(item => item.Ingredient)
            .Include(product => product.Variations)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.Product)
            .Include(product => product.ComboGroups)
                .ThenInclude(group => group.Options)
                    .ThenInclude(option => option.ProductVariation)
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
    /// Reemplaza la colección sin cargarla junto al agregado principal, evitando conflictos
    /// entre instancias rastreadas y manteniendo compatibilidad con proveedores de pruebas.
    /// </summary>
    public async Task DeleteRecipeItemsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        var items = await context.ProductRecipeItems
            .Where(item => item.TenantId == tenantId && item.ProductId == productId)
            .ToArrayAsync(cancellationToken);
        context.ProductRecipeItems.RemoveRange(items);
    }

    public async Task AddRecipeItemsAsync(IEnumerable<ProductRecipeItem> items, CancellationToken cancellationToken)
        => await context.ProductRecipeItems.AddRangeAsync(items, cancellationToken);

    public async Task<IReadOnlyCollection<ProductVariation>> GetVariationsForUpdateAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
        => await context.ProductVariations
            .Where(item => item.TenantId == tenantId && item.ProductId == productId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetVariationIdsUsedInComboAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken)
        => (await context.ProductComboOptions.AsNoTracking()
            .Where(option => option.TenantId == tenantId
                && option.ProductId == productId
                && option.ProductVariationId.HasValue)
            .Select(option => option.ProductVariationId!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken))
            .ToHashSet();

    public async Task AddVariationsAsync(IEnumerable<ProductVariation> variations, CancellationToken cancellationToken)
        => await context.ProductVariations.AddRangeAsync(variations, cancellationToken);

    public void RemoveVariations(IEnumerable<ProductVariation> variations)
        => context.ProductVariations.RemoveRange(variations);

    public async Task DeleteComboGroupsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        var groups = await context.ProductComboGroups
            .Where(group => group.TenantId == tenantId && group.ComboProductId == productId)
            .ToArrayAsync(cancellationToken);
        context.ProductComboGroups.RemoveRange(groups);
    }

    public async Task AddComboGroupsAsync(IEnumerable<ProductComboGroup> groups, CancellationToken cancellationToken)
        => await context.ProductComboGroups.AddRangeAsync(groups, cancellationToken);

    public Task<bool> IsUsedInComboAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
        => context.ProductComboOptions.AnyAsync(
            option => option.TenantId == tenantId && option.ProductId == productId,
            cancellationToken);

    public void Remove(Product product) => context.Products.Remove(product);
}
