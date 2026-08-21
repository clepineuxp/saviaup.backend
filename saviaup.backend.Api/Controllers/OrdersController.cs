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
    IGetActiveTableOrderUseCase getActiveUseCase,
    IAddTableOrderItemsUseCase addItemsUseCase,
    IMoveTableOrderUseCase moveUseCase,
    ICancelOrderItemUseCase cancelItemUseCase,
    IPayAndCloseTableOrderUseCase checkoutUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
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
        => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? "usuario";
}
