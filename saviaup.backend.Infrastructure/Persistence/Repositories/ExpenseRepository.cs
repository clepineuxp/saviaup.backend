using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository(ApplicationDbContext dbContext, IDateTimeProvider clock) : IExpenseRepository
{
    public async Task<PageData<Expense>> GetPageAsync(
        Guid tenantId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? search,
        Guid? supplierId,
        string? status,
        string? paymentMethod,
        bool? isCashOut,
        int page,
        int pageSize,
        CancellationToken cancellationToken, bool ByCreatedAt = false, DateOnly? FromBusinessDate = null, DateOnly? ToBusinessDate = null)
    {
        var query = dbContext.Expenses
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x => x.TenantId == tenantId);

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.ToUniversalTime();
            query = query.Where(x => ByCreatedAt ? x.CreatedAt >= fromUtc : (x.BusinessDate.HasValue && FromBusinessDate.HasValue ? x.BusinessDate >= FromBusinessDate : x.ExpenseDate >= fromUtc));
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.ToUniversalTime();
            query = query.Where(x => ByCreatedAt ? x.CreatedAt < toUtc : (x.BusinessDate.HasValue && ToBusinessDate.HasValue ? x.BusinessDate < ToBusinessDate : x.ExpenseDate < toUtc));
        }

        if (supplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            query = query.Where(x => x.PaymentMethod == paymentMethod);
        }

        if (isCashOut.HasValue)
        {
            query = query.Where(x => x.IsCashOut == isCashOut.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.NormalizedName.Contains(cleanSearch) ||
                (x.Description != null && x.Description.ToUpper().Contains(cleanSearch)) ||
                (x.Supplier != null && x.Supplier.NormalizedName.Contains(cleanSearch)) ||
                x.ConsecutiveNumber.ToString().Contains(cleanSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.ConsecutiveNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PageData<Expense>(items, totalCount);
    }

    public async Task<Expense?> GetByIdAsync(Guid tenantId, Guid expenseId, CancellationToken cancellationToken)
    {
        return await dbContext.Expenses
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == expenseId, cancellationToken);
    }

    public async Task<long> GetNextConsecutiveAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        const string parameterKey = "expenses.nextConsecutive";

        if (dbContext.Database.IsNpgsql())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({tenantId.ToString()}, 0));",
                cancellationToken);
        }

        var highestConsecutive = await dbContext.Expenses
            .Where(x => x.TenantId == tenantId)
            .MaxAsync(x => (long?)x.ConsecutiveNumber, cancellationToken) ?? 0;

        var param = await dbContext.OrganizationParameters
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Key == parameterKey, cancellationToken);

        var configuredConsecutive = param is not null
            && long.TryParse(param.Value, out var parsedValue)
            && parsedValue > 0
                ? parsedValue
                : 1;
        var currentConsecutive = Math.Max(configuredConsecutive, highestConsecutive + 1);
        var now = clock.UtcNow;

        if (param is null)
        {
            param = new OrganizationParameter
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = parameterKey,
                Value = (currentConsecutive + 1).ToString(),
                ValueType = "integer",
                CreatedAt = now,
                UpdatedAt = now
            };
            await dbContext.OrganizationParameters.AddAsync(param, cancellationToken);
        }
        else
        {
            param.Value = (currentConsecutive + 1).ToString();
            param.UpdatedAt = now;
        }

        return currentConsecutive;
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken)
    {
        await dbContext.Expenses.AddAsync(expense, cancellationToken);
    }
}
