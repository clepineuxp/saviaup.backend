using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Authorize]
[RequireTenant]
[Route("api/inventory")]
public sealed class InventoryController(IListInventoryUseCase listUseCase, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.InventoryStockRead)]
    public async Task<ActionResult<PagedResponse<InventoryItemDto>>> List(
        [FromQuery] InventoryQueryRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await listUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));
}
