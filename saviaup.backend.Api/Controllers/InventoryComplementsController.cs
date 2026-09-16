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
[Route("api/inventory/complements")]
public sealed class InventoryComplementsController(
    IListMeasurementUnitsUseCase listUseCase,
    ICreateMeasurementUnitUseCase createUseCase,
    IUpdateMeasurementUnitUseCase updateUseCase,
    ISetMeasurementUnitStatusUseCase statusUseCase,
    IDeleteMeasurementUnitUseCase deleteUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("units")]
    [RequirePermission(PermissionCodes.InventoryComplementsRead)]
    public async Task<ActionResult<PagedResponse<MeasurementUnitDto>>> ListUnits(
        [FromQuery] MeasurementUnitQueryRequest request, CancellationToken cancellationToken)
        => this.FromResult(await listUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPost("units")]
    [RequirePermission(PermissionCodes.InventoryComplementsManage)]
    public async Task<ActionResult<MeasurementUnitDto>> CreateUnit(
        CreateMeasurementUnitRequest request, CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPut("units/{unitId:guid}")]
    [RequirePermission(PermissionCodes.InventoryComplementsManage)]
    public async Task<ActionResult<MeasurementUnitDto>> UpdateUnit(
        Guid unitId, UpdateMeasurementUnitRequest request, CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(currentUser.TenantId!.Value, unitId, request, cancellationToken));

    [HttpPatch("units/{unitId:guid}/status")]
    [RequirePermission(PermissionCodes.InventoryComplementsManage)]
    public async Task<ActionResult<MeasurementUnitDto>> SetUnitStatus(
        Guid unitId, SetMeasurementUnitStatusRequest request, CancellationToken cancellationToken)
        => this.FromResult(await statusUseCase.ExecuteAsync(currentUser.TenantId!.Value, unitId, request, cancellationToken));

    [HttpDelete("units/{unitId:guid}")]
    [RequirePermission(PermissionCodes.InventoryComplementsManage)]
    public async Task<ActionResult> DeleteUnit(Guid unitId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(currentUser.TenantId!.Value, unitId, cancellationToken));
}
