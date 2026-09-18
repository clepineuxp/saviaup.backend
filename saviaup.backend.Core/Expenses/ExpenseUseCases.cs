using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Expenses;

public sealed class GetExpensesUseCase(
    IExpenseRepository repository, IOrganizationTimeZone organizationTimeZone, ITimeZoneService timeZones) : IGetExpensesUseCase
{
    public async Task<Result<ExpensePageDto>> ExecuteAsync(
        Guid tenantId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? search,
        Guid? supplierId,
        string? status,
        string? paymentMethod,
        bool? isCashOut,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (fromDate > toDate || toDate == DateOnly.MaxValue) return Result<ExpensePageDto>.Failure(Errors.Validation);
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var fromUtc = fromDate is { } start ? timeZones.StartOfDayUtc(start, zone) : (DateTimeOffset?)null;
        var toUtc = toDate is { } end ? timeZones.StartOfDayUtc(end.AddDays(1), zone) : (DateTimeOffset?)null;
        var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var cleanStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        var cleanPaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? null : paymentMethod.Trim();
        var p = page < 1 ? 1 : page;
        var ps = pageSize is < 1 or > 100 ? 20 : pageSize;

        var pageData = await repository.GetPageAsync(
            tenantId, fromUtc, toUtc, cleanSearch, supplierId, cleanStatus, cleanPaymentMethod, isCashOut, p, ps, cancellationToken, FromBusinessDate: fromDate, ToBusinessDate: toDate?.AddDays(1));

        var dtos = pageData.Items.Select(ExpenseRules.ToDto).ToArray();
        var totalPages = (int)Math.Ceiling(pageData.TotalCount / (double)ps);

        return Result<ExpensePageDto>.Success(new ExpensePageDto(dtos, p, ps, pageData.TotalCount, totalPages));
    }
}

public sealed class CreateExpenseUseCase(
    IExpenseRepository repository,
    ISupplierRepository supplierRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork, IOrganizationTimeZone organizationTimeZone, ITimeZoneService timeZones) : ICreateExpenseUseCase
{
    public async Task<Result<ExpenseDto>> ExecuteAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var businessDate = request.BusinessDate ?? DateOnly.FromDateTime(timeZones.ConvertFromUtc(request.ExpenseDate ?? now, zone).DateTime);
        if (!ExpenseRules.TryPrepare(
                request.Name,
                request.Description,
                request.Amount,
                request.IsCashOut,
                request.PaymentMethod,
                request.SupplierId,
                request.ExpenseDate ?? timeZones.StartOfDayUtc(businessDate, zone),
                now,
                out var values))
        {
            return Result<ExpenseDto>.Failure(Errors.Validation);
        }

        Supplier? supplier = null;
        if (values.SupplierId.HasValue)
        {
            supplier = await supplierRepository.GetByIdAsync(tenantId, values.SupplierId.Value, cancellationToken);
            if (supplier is null) return Result<ExpenseDto>.Failure(Errors.SupplierNotFound);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var consecutive = await repository.GetNextConsecutiveAsync(tenantId, transactionToken);

            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConsecutiveNumber = consecutive,
                Name = values.Name,
                NormalizedName = values.NormalizedName,
                Description = values.Description,
                Amount = values.Amount,
                IsCashOut = values.IsCashOut,
                PaymentMethod = values.PaymentMethod,
                SupplierId = supplier?.Id,
                Supplier = supplier,
                ExpenseDate = values.ExpenseDate,
                BusinessDate = businessDate,
                Status = "ACTIVE",
                CreatedByUserId = userId,
                CreatedByUserName = userName,
                LastModifiedByUserId = userId,
                LastModifiedByUserName = userName,
                CreatedAt = now,
                UpdatedAt = now
            };

            await repository.AddAsync(expense, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            return Result<ExpenseDto>.Success(ExpenseRules.ToDto(expense));
        }, cancellationToken);
    }
}

public sealed class UpdateExpenseUseCase(
    IExpenseRepository repository,
    ISupplierRepository supplierRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork, IOrganizationTimeZone organizationTimeZone, ITimeZoneService timeZones) : IUpdateExpenseUseCase
{
    public async Task<Result<ExpenseDto>> ExecuteAsync(
        Guid tenantId,
        Guid expenseId,
        Guid userId,
        string userName,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var businessDate = request.BusinessDate ?? DateOnly.FromDateTime(timeZones.ConvertFromUtc(request.ExpenseDate ?? now, zone).DateTime);
        if (!ExpenseRules.TryPrepare(
                request.Name,
                request.Description,
                request.Amount,
                request.IsCashOut,
                request.PaymentMethod,
                request.SupplierId,
                request.ExpenseDate ?? timeZones.StartOfDayUtc(businessDate, zone),
                now,
                out var values))
        {
            return Result<ExpenseDto>.Failure(Errors.Validation);
        }

        var expense = await repository.GetByIdAsync(tenantId, expenseId, cancellationToken);
        if (expense is null) return Result<ExpenseDto>.Failure(Errors.ExpenseNotFound);

        if (string.Equals(expense.Status, "ANNULLED", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ExpenseDto>.Failure(Errors.ExpenseAlreadyAnnulled);
        }

        Supplier? supplier = null;
        if (values.SupplierId.HasValue)
        {
            supplier = await supplierRepository.GetByIdAsync(tenantId, values.SupplierId.Value, cancellationToken);
            if (supplier is null) return Result<ExpenseDto>.Failure(Errors.SupplierNotFound);
        }

        expense.Name = values.Name;
        expense.NormalizedName = values.NormalizedName;
        expense.Description = values.Description;
        expense.Amount = values.Amount;
        expense.IsCashOut = values.IsCashOut;
        expense.PaymentMethod = values.PaymentMethod;
        expense.SupplierId = supplier?.Id;
        expense.Supplier = supplier;
        expense.ExpenseDate = values.ExpenseDate;
        expense.BusinessDate = businessDate;
        expense.LastModifiedByUserId = userId;
        expense.LastModifiedByUserName = userName;
        expense.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ExpenseDto>.Success(ExpenseRules.ToDto(expense));
    }
}

public sealed class AnnulExpenseUseCase(
    IExpenseRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IAnnulExpenseUseCase
{
    public async Task<Result<ExpenseDto>> ExecuteAsync(
        Guid tenantId,
        Guid expenseId,
        Guid userId,
        string userName,
        AnnulExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var expense = await repository.GetByIdAsync(tenantId, expenseId, cancellationToken);
        if (expense is null) return Result<ExpenseDto>.Failure(Errors.ExpenseNotFound);

        if (string.Equals(expense.Status, "ANNULLED", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ExpenseDto>.Failure(Errors.ExpenseAlreadyAnnulled);
        }

        var now = clock.UtcNow;
        expense.Status = "ANNULLED";
        expense.AnnulledReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        expense.AnnulledAt = now;
        expense.AnnulledByUserId = userId;
        expense.AnnulledByUserName = userName;
        expense.LastModifiedByUserId = userId;
        expense.LastModifiedByUserName = userName;
        expense.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ExpenseDto>.Success(ExpenseRules.ToDto(expense));
    }
}
