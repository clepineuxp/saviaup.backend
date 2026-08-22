using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[RequireTenant]
public sealed class OrdersController(
    IGetOrdersPageUseCase getOrdersPageUseCase,
    IGetOrderItemsPageUseCase getOrderItemsPageUseCase,
    IGetActiveTableOrderUseCase getActiveUseCase,
    IAddTableOrderItemsUseCase addItemsUseCase,
    IMoveTableOrderUseCase moveUseCase,
    ICancelOrderItemUseCase cancelItemUseCase,
    IPayAndCloseTableOrderUseCase checkoutUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.OrdersRead)]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string[]? statuses = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] Guid? tableId = null,
        CancellationToken cancellationToken = default)
    {
        var req = new OrderQueryRequest(page, pageSize, search, statuses, fromDate, toDate, tableId);
        return this.FromResult(await getOrdersPageUseCase.ExecuteAsync(currentUser.TenantId!.Value, req, cancellationToken));
    }

    [HttpGet("items")]
    [RequirePermission(PermissionCodes.OrdersRead)]
    public async Task<ActionResult<PagedResponse<OrderItemReportDto>>> GetOrderItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string[]? statuses = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] Guid? tableId = null,
        CancellationToken cancellationToken = default)
    {
        var req = new OrderQueryRequest(page, pageSize, search, statuses, fromDate, toDate, tableId);
        return this.FromResult(await getOrderItemsPageUseCase.ExecuteAsync(currentUser.TenantId!.Value, req, cancellationToken));
    }

    [HttpGet("table/{tableId:guid}/active")]
    [RequirePermission(PermissionCodes.TablesRead)]
    public async Task<ActionResult<OrderDto>> GetActiveTableOrder(
        Guid tableId,
        CancellationToken cancellationToken)
        => this.FromResult(await getActiveUseCase.ExecuteAsync(currentUser.TenantId!.Value, tableId, cancellationToken));

    [HttpPost("table/{tableId:guid}/items")]
    [RequirePermission(PermissionCodes.TablesOperate)]
    public async Task<ActionResult<OrderDto>> AddTableOrderItems(
        Guid tableId,
        [FromBody] AddOrderItemsRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await addItemsUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            tableId,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    [HttpPost("table/{tableId:guid}/move")]
    [RequirePermission(PermissionCodes.TablesOperate)]
    public async Task<ActionResult<RestaurantTableDto>> MoveTableOrder(
        Guid tableId,
        [FromBody] MoveTableOrderRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await moveUseCase.ExecuteAsync(currentUser.TenantId!.Value, tableId, request, cancellationToken));

    [HttpPost("items/{itemId:guid}/cancel")]
    [RequirePermission(PermissionCodes.TablesOperate)]
    public async Task<ActionResult<OrderDto>> CancelOrderItem(
        Guid itemId,
        [FromBody] CancelOrderItemRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await cancelItemUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            itemId,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    [HttpPost("table/{tableId:guid}/checkout")]
    [RequirePermission(PermissionCodes.TablesOperate)]
    public async Task<ActionResult<OrderDto>> PayAndCloseTableOrder(
        Guid tableId,
        [FromBody] CheckoutOrderRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await checkoutUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            tableId,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    private string GetUserName()
    {
        if (!string.IsNullOrWhiteSpace(currentUser.UserEmail)) return currentUser.UserEmail;

        var emailClaim = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? User.FindFirstValue("email");
        if (!string.IsNullOrWhiteSpace(emailClaim)) return emailClaim;

        var nameClaim = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
        if (!string.IsNullOrWhiteSpace(nameClaim)) return nameClaim;

        var givenName = User.FindFirstValue(ClaimTypes.GivenName);
        var surname = User.FindFirstValue(ClaimTypes.Surname);
        var fullName = $"{givenName} {surname}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName)) return fullName;

        return currentUser.UserId?.ToString() ?? "usuario@saviaup.com";
    }
}
