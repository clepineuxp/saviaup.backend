using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(SaviaUpDbContext context) : IProductRepository
{
    public async Task<PageData<Product>> GetPageAsync(
        Guid tenantId,
        ProductQueryRequest request,
        ProductType? type,
        CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking()
            .Include(product => product.Category)
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
            .SingleOrDefaultAsync(
                product => product.TenantId == tenantId && product.Id == productId,
                cancellationToken);

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

    public void Remove(Product product) => context.Products.Remove(product);
}
