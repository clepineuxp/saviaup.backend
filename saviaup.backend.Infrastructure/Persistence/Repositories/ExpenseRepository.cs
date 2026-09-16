using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository(ApplicationDbContext dbContext) : IExpenseRepository
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
        CancellationToken cancellationToken)
    {
        var query = dbContext.Expenses
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x => x.TenantId == tenantId);

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.ToUniversalTime();
            query = query.Where(x => x.ExpenseDate >= fromUtc || x.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.ToUniversalTime();
            query = query.Where(x => x.ExpenseDate <= toUtc || x.CreatedAt <= toUtc);
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
        var param = await dbContext.OrganizationParameters
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Key == parameterKey, cancellationToken);

        long currentConsecutive = 1;
        var now = DateTimeOffset.UtcNow;

        if (param is null)
        {
            param = new OrganizationParameter
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = parameterKey,
                Value = "2",
                ValueType = "integer",
                CreatedAt = now,
                UpdatedAt = now
            };
            await dbContext.OrganizationParameters.AddAsync(param, cancellationToken);
        }
        else
        {
            if (long.TryParse(param.Value, out var parsedValue) && parsedValue > 0)
            {
                currentConsecutive = parsedValue;
            }
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
