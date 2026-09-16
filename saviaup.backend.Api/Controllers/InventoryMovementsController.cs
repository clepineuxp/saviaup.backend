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
[Route("api/inventory/movements")]
public sealed class InventoryMovementsController(
    IListInventoryMovementsUseCase listUseCase,
    ICreateInventoryMovementUseCase createUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.InventoryMovementsRead)]
    public async Task<ActionResult<PagedResponse<InventoryMovementDto>>> List(
        [FromQuery] InventoryMovementQueryRequest request, CancellationToken cancellationToken)
        => this.FromResult(await listUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.InventoryMovementsManage)]
    public async Task<ActionResult<InventoryMovementDto>> Create(
        CreateInventoryMovementRequest request, CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(
            currentUser.TenantId!.Value, currentUser.UserId!.Value, request, cancellationToken));
}
