using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Billing;

public sealed class GetBillingReceiptsUseCase(
    IBillingRepository repository,
    IDateTimeProvider clock, IOrganizationTimeZone organizationTimeZone, ITimeZoneService timeZones) : IGetBillingReceiptsUseCase
{
    public async Task<Result<PagedResponse<BillingReceiptItemDto>>> ExecuteAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FromLocalDate > request.ToLocalDate || request.ToLocalDate == DateOnly.MaxValue) return Result<PagedResponse<BillingReceiptItemDto>>.Failure(Errors.Validation);
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var normalizedRequest = NormalizeDates(request, clock.UtcNow, zone, timeZones);
        var page = await repository.GetReceiptsPageAsync(tenantId, normalizedRequest, cancellationToken);
        var totalPages = (int)Math.Ceiling(page.TotalCount / (double)normalizedRequest.PageSize);
        var response = new PagedResponse<BillingReceiptItemDto>(page.Items, normalizedRequest.Page, normalizedRequest.PageSize, page.TotalCount, totalPages);
        return Result<PagedResponse<BillingReceiptItemDto>>.Success(response);
    }

    internal static BillingReceiptQueryRequest NormalizeDates(BillingReceiptQueryRequest req, DateTimeOffset now, string zone, ITimeZoneService timeZones)
    {
        var today = DateOnly.FromDateTime(timeZones.ConvertFromUtc(now, zone).DateTime);
        var fromDate = req.FromDate ?? timeZones.StartOfDayUtc(req.FromLocalDate ?? today, zone);
        var toDate = req.ToDate ?? timeZones.StartOfDayUtc((req.ToLocalDate ?? today).AddDays(1), zone);
        return req with { FromDate = fromDate, ToDate = toDate, Page = Math.Max(1, req.Page), PageSize = Math.Clamp(req.PageSize, 1, 100) };
    }
}

public sealed class GetBillingOrdersUseCase(
    IBillingRepository repository,
    IDateTimeProvider clock, IOrganizationTimeZone organizationTimeZone, ITimeZoneService timeZones) : IGetBillingOrdersUseCase
{
    public async Task<Result<PagedResponse<BillingOrderDto>>> ExecuteAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FromLocalDate > request.ToLocalDate || request.ToLocalDate == DateOnly.MaxValue) return Result<PagedResponse<BillingOrderDto>>.Failure(Errors.Validation);
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var normalizedRequest = GetBillingReceiptsUseCase.NormalizeDates(request, clock.UtcNow, zone, timeZones);
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
