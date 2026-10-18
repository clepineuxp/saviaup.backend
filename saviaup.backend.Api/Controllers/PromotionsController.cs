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
[Route("api/promotions")]
public sealed class PromotionsController(
    IListPromotionsUseCase listUseCase,
    ICreatePromotionUseCase createUseCase,
    IUpdatePromotionUseCase updateUseCase,
    ISetPromotionStatusUseCase statusUseCase,
    IDeletePromotionUseCase deleteUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.PromotionsRead, PermissionCodes.PromotionsManage)]
    public async Task<ActionResult<IReadOnlyCollection<PromotionDto>>> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.FromResult(await listUseCase.ExecuteAsync(currentUser.TenantId!.Value, includeInactive, cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.PromotionsManage)]
    public async Task<ActionResult<PromotionDto>> Create(CreatePromotionRequest request, CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPut("{promotionId:guid}")]
    [RequirePermission(PermissionCodes.PromotionsManage)]
    public async Task<ActionResult<PromotionDto>> Update(Guid promotionId, UpdatePromotionRequest request, CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(currentUser.TenantId!.Value, promotionId, request, cancellationToken));

    [HttpPatch("{promotionId:guid}/status")]
    [RequirePermission(PermissionCodes.PromotionsManage)]
    public async Task<ActionResult<PromotionDto>> SetStatus(Guid promotionId, SetPromotionStatusRequest request, CancellationToken cancellationToken)
        => this.FromResult(await statusUseCase.ExecuteAsync(currentUser.TenantId!.Value, promotionId, request, cancellationToken));

    [HttpDelete("{promotionId:guid}")]
    [RequirePermission(PermissionCodes.PromotionsManage)]
    public async Task<ActionResult> Delete(Guid promotionId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(currentUser.TenantId!.Value, promotionId, cancellationToken));
}
