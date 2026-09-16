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
[Route("api/tables")]
public sealed class RestaurantTablesController(
    IListRestaurantTablesUseCase listUseCase,
    ICreateRestaurantTableUseCase createUseCase,
    IUpdateRestaurantTableUseCase updateUseCase,
    IDeleteRestaurantTableUseCase deleteUseCase,
    IGetTableOperationUseCase operationUseCase,
    ISetTableOperationUseCase setOperationUseCase,
    IUpdateTableOrderUseCase updateOrderUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.TablesManage)]
    public async Task<ActionResult<IReadOnlyCollection<RestaurantTableDto>>> List(
        [FromQuery] Guid? areaId,
        CancellationToken cancellationToken)
        => this.FromResult(await listUseCase.ExecuteAsync(currentUser.TenantId!.Value, areaId, cancellationToken));

    [HttpGet("operation")]
    [RequirePermission(PermissionCodes.TablesRead)]
    public async Task<ActionResult<TableOperationSnapshotDto>> Operation(CancellationToken cancellationToken)
        => this.FromResult(await operationUseCase.ExecuteAsync(currentUser.TenantId!.Value, cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.TablesManage)]
    public async Task<ActionResult<RestaurantTableDto>> Create(
        CreateRestaurantTableRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPut("{tableId:guid}")]
    [RequirePermission(PermissionCodes.TablesManage)]
    public async Task<ActionResult<RestaurantTableDto>> Update(
        Guid tableId,
        UpdateRestaurantTableRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(currentUser.TenantId!.Value, tableId, request, cancellationToken));

    [HttpPatch("{tableId:guid}/operation")]
    [RequirePermission(PermissionCodes.TablesOperate)]
    public async Task<ActionResult<RestaurantTableDto>> SetOperation(
        Guid tableId,
        SetTableOperationRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await setOperationUseCase.ExecuteAsync(currentUser.TenantId!.Value, tableId, request, cancellationToken));

    [HttpPatch("{tableId:guid}/order")]
    [RequirePermission(PermissionCodes.TablesOperate)]
    public async Task<ActionResult<RestaurantTableDto>> UpdateOrder(
        Guid tableId,
        UpdateTableOrderRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateOrderUseCase.ExecuteAsync(currentUser.TenantId!.Value, tableId, request, cancellationToken));

    [HttpDelete("{tableId:guid}")]
    [RequirePermission(PermissionCodes.TablesManage)]
    public async Task<ActionResult> Delete(Guid tableId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(currentUser.TenantId!.Value, tableId, cancellationToken));
}
