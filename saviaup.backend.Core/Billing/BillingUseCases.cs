using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Billing;

public sealed class GetBillingReceiptsUseCase(
    IBillingRepository repository,
    IDateTimeProvider clock) : IGetBillingReceiptsUseCase
{
    public async Task<Result<PagedResponse<BillingReceiptItemDto>>> ExecuteAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = NormalizeDates(request, clock.UtcNow);
        var page = await repository.GetReceiptsPageAsync(tenantId, normalizedRequest, cancellationToken);
        var totalPages = (int)Math.Ceiling(page.TotalCount / (double)normalizedRequest.PageSize);
        var response = new PagedResponse<BillingReceiptItemDto>(page.Items, normalizedRequest.Page, normalizedRequest.PageSize, page.TotalCount, totalPages);
        return Result<PagedResponse<BillingReceiptItemDto>>.Success(response);
    }

    internal static BillingReceiptQueryRequest NormalizeDates(BillingReceiptQueryRequest req, DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        var fromDate = req.FromDate.HasValue
            ? req.FromDate.Value.ToUniversalTime()
            : new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, TimeSpan.Zero);

        var toDate = req.ToDate.HasValue
            ? req.ToDate.Value.ToUniversalTime()
            : new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 23, 59, 59, 999, TimeSpan.Zero);

        return req with { FromDate = fromDate, ToDate = toDate };
    }
}

public sealed class GetBillingOrdersUseCase(
    IBillingRepository repository,
    IDateTimeProvider clock) : IGetBillingOrdersUseCase
{
    public async Task<Result<PagedResponse<BillingOrderDto>>> ExecuteAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = GetBillingReceiptsUseCase.NormalizeDates(request, clock.UtcNow);
        var page = await repository.GetOrdersPageAsync(tenantId, normalizedRequest, cancellationToken);
        var totalPages = (int)Math.Ceiling(page.TotalCount / (double)normalizedRequest.PageSize);
        var response = new PagedResponse<BillingOrderDto>(page.Items, normalizedRequest.Page, normalizedRequest.PageSize, page.TotalCount, totalPages);
        return Result<PagedResponse<BillingOrderDto>>.Success(response);
    }
}

public sealed class GetBillingReceiptByIdUseCase(
    IBillingRepository repository) : IGetBillingReceiptByIdUseCase
{
    public async Task<Result<OrderReceiptDto>> ExecuteAsync(
        Guid tenantId,
        Guid receiptId,
        CancellationToken cancellationToken)
    {
        var receipt = await repository.GetReceiptByIdAsync(tenantId, receiptId, cancellationToken);
        if (receipt is null)
        {
            return Result<OrderReceiptDto>.Failure(Errors.Validation);
        }

        return Result<OrderReceiptDto>.Success(receipt);
    }
}
