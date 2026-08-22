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
[Authorize]
[RequireTenant]
[Route("api/cash-registers")]
public sealed class CashRegistersController(
    IListCashRegistersUseCase listUseCase,
    ICreateCashRegisterUseCase createUseCase,
    IUpdateCashRegisterUseCase updateUseCase,
    ISetCashRegisterStatusUseCase setStatusUseCase,
    IDeleteCashRegisterUseCase deleteUseCase,
    IOpenCashRegisterShiftUseCase openShiftUseCase,
    ICloseCashRegisterShiftUseCase closeShiftUseCase,
    IGetCashRegisterShiftSummaryUseCase getShiftSummaryUseCase,
    IListCashRegisterShiftsUseCase listShiftsUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.CashRegistersRead)]
    public async Task<ActionResult<IReadOnlyCollection<CashRegisterDto>>> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.FromResult(await listUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            includeInactive,
            cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.CashRegistersManage)]
    public async Task<ActionResult<CashRegisterDto>> Create(
        CreateCashRegisterRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            request,
            cancellationToken));

    [HttpPut("{cashRegisterId:guid}")]
    [RequirePermission(PermissionCodes.CashRegistersManage)]
    public async Task<ActionResult<CashRegisterDto>> Update(
        Guid cashRegisterId,
        UpdateCashRegisterRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            cashRegisterId,
            request,
            cancellationToken));

    [HttpPatch("{cashRegisterId:guid}/status")]
    [RequirePermission(PermissionCodes.CashRegistersManage)]
    public async Task<ActionResult<CashRegisterDto>> SetStatus(
        Guid cashRegisterId,
        SetCashRegisterStatusRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await setStatusUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            cashRegisterId,
            request,
            cancellationToken));

    [HttpDelete("{cashRegisterId:guid}")]
    [RequirePermission(PermissionCodes.CashRegistersManage)]
    public async Task<ActionResult> Delete(Guid cashRegisterId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            cashRegisterId,
            cancellationToken));

    [HttpPost("shifts/open")]
    [RequirePermission(PermissionCodes.CashRegistersOperate)]
    public async Task<ActionResult<CashRegisterShiftDto>> OpenShift(
        OpenCashRegisterShiftRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var userName = User.FindFirstValue(ClaimTypes.Email) ?? userId.ToString();
        return this.FromResult(await openShiftUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            userId,
            userName,
            request,
            cancellationToken));
    }

    [HttpPost("shifts/{shiftId:guid}/close")]
    [RequirePermission(PermissionCodes.CashRegistersOperate)]
    public async Task<ActionResult<CashRegisterShiftDto>> CloseShift(
        Guid shiftId,
        CloseCashRegisterShiftRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var userName = User.FindFirstValue(ClaimTypes.Email) ?? userId.ToString();
        return this.FromResult(await closeShiftUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            shiftId,
            userId,
            userName,
            request,
            cancellationToken));
    }

    [HttpGet("shifts/{shiftId:guid}/summary")]
    [RequirePermission(PermissionCodes.CashRegistersRead)]
    public async Task<ActionResult<CashRegisterShiftSummaryDto>> GetShiftSummary(
        Guid shiftId,
        CancellationToken cancellationToken)
        => this.FromResult(await getShiftSummaryUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            shiftId,
            cancellationToken));

    [HttpGet("shifts")]
    [RequirePermission(PermissionCodes.CashRegistersRead)]
    public async Task<ActionResult<PagedResponse<CashRegisterShiftDto>>> ListShifts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] Guid? cashRegisterId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var req = new CashRegisterShiftQueryRequest(page, pageSize, cashRegisterId, status);
        return this.FromResult(await listShiftsUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            req,
            cancellationToken));
    }
}
