using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
[RequireTenant]
public sealed class BillingController(
    ICurrentUserContext currentUser,
    IGetBillingReceiptsUseCase getBillingReceiptsUseCase,
    IGetBillingOrdersUseCase getBillingOrdersUseCase,
    IGetBillingReceiptByIdUseCase getBillingReceiptByIdUseCase) : ControllerBase
{
    [HttpGet("receipts")]
    [RequirePermission([PermissionCodes.BillingRead, PermissionCodes.BillingManage])]
    public async Task<ActionResult<PagedResponse<BillingReceiptItemDto>>> GetReceipts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var req = new BillingReceiptQueryRequest(page, pageSize, search, fromDate, toDate);
        var result = await getBillingReceiptsUseCase.ExecuteAsync(currentUser.TenantId!.Value, req, cancellationToken);
        return this.FromResult(result);
    }

    [HttpGet("orders")]
    [RequirePermission([PermissionCodes.BillingRead, PermissionCodes.BillingManage])]
    public async Task<ActionResult<PagedResponse<BillingOrderDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var req = new BillingReceiptQueryRequest(page, pageSize, search, fromDate, toDate);
        var result = await getBillingOrdersUseCase.ExecuteAsync(currentUser.TenantId!.Value, req, cancellationToken);
        return this.FromResult(result);
    }

    [HttpGet("receipts/{id:guid}")]
    [RequirePermission([PermissionCodes.BillingRead, PermissionCodes.BillingManage])]
    public async Task<ActionResult<OrderReceiptDto>> GetReceiptById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await getBillingReceiptByIdUseCase.ExecuteAsync(currentUser.TenantId!.Value, id, cancellationToken);
        return this.FromResult(result);
    }
}
