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
[Route("api/inventory/ingredients")]
public sealed class IngredientsController(
    IListIngredientsUseCase listUseCase,
    ICreateIngredientUseCase createUseCase,
    IUpdateIngredientUseCase updateUseCase,
    ISetIngredientStatusUseCase statusUseCase,
    IDeleteIngredientUseCase deleteUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.InventoryIngredientsRead)]
    public async Task<ActionResult<PagedResponse<IngredientDto>>> List(
        [FromQuery] IngredientQueryRequest request, CancellationToken cancellationToken)
        => this.FromResult(await listUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.InventoryIngredientsManage)]
    public async Task<ActionResult<IngredientDto>> Create(CreateIngredientRequest request, CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(
            currentUser.TenantId!.Value, currentUser.UserId!.Value, request, cancellationToken));

    [HttpPut("{ingredientId:guid}")]
    [RequirePermission(PermissionCodes.InventoryIngredientsManage)]
    public async Task<ActionResult<IngredientDto>> Update(
        Guid ingredientId, UpdateIngredientRequest request, CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(
            currentUser.TenantId!.Value, ingredientId, request, cancellationToken));

    [HttpPatch("{ingredientId:guid}/status")]
    [RequirePermission(PermissionCodes.InventoryIngredientsManage)]
    public async Task<ActionResult<IngredientDto>> SetStatus(
        Guid ingredientId, SetIngredientStatusRequest request, CancellationToken cancellationToken)
        => this.FromResult(await statusUseCase.ExecuteAsync(
            currentUser.TenantId!.Value, ingredientId, request, cancellationToken));

    [HttpDelete("{ingredientId:guid}")]
    [RequirePermission(PermissionCodes.InventoryIngredientsManage)]
    public async Task<ActionResult> Delete(Guid ingredientId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(currentUser.TenantId!.Value, ingredientId, cancellationToken));
}
