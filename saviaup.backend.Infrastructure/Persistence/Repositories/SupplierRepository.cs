using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

internal sealed class SupplierRepository(ApplicationDbContext dbContext) : ISupplierRepository
{
    public async Task<PageData<Supplier>> GetPageAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Suppliers.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.NormalizedName.Contains(cleanSearch) ||
                (x.CommercialName != null && x.CommercialName.ToUpper().Contains(cleanSearch)) ||
                (x.Document != null && x.Document.Contains(cleanSearch)) ||
                (x.Email != null && x.Email.ToUpper().Contains(cleanSearch)) ||
                (x.Phone != null && x.Phone.Contains(cleanSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PageData<Supplier>(items, totalCount);
    }

    public async Task<IReadOnlyCollection<Supplier>> GetLookupAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(Guid tenantId, Guid supplierId, CancellationToken cancellationToken)
    {
        return await dbContext.Suppliers
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == supplierId, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(Guid tenantId, string normalizedName, Guid? excludedSupplierId, CancellationToken cancellationToken)
    {
        return await dbContext.Suppliers
            .AnyAsync(x => x.TenantId == tenantId && x.NormalizedName == normalizedName && x.Id != excludedSupplierId, cancellationToken);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken)
    {
        await dbContext.Suppliers.AddAsync(supplier, cancellationToken);
    }
}
