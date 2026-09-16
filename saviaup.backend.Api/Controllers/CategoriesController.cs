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
[Route("api/categories")]
public sealed class CategoriesController(
    IListCategoriesUseCase listUseCase,
    ICreateCategoryUseCase createUseCase,
    IUpdateCategoryUseCase updateUseCase,
    ISetCategoryStatusUseCase setStatusUseCase,
    IDeleteCategoryUseCase deleteUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.CategoriesRead, PermissionCodes.OrdersCreate, PermissionCodes.OrdersRead, PermissionCodes.TablesOperate, PermissionCodes.TablesRead)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryDto>>> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => this.FromResult(await listUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            includeInactive,
            cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            request,
            cancellationToken));

    [HttpPut("{categoryId:guid}")]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<ActionResult<CategoryDto>> Update(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            categoryId,
            request,
            cancellationToken));

    [HttpPatch("{categoryId:guid}/status")]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<ActionResult<CategoryDto>> SetStatus(
        Guid categoryId,
        SetCategoryStatusRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await setStatusUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            categoryId,
            request,
            cancellationToken));

    [HttpDelete("{categoryId:guid}")]
    [RequirePermission(PermissionCodes.CategoriesManage)]
    public async Task<ActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            categoryId,
            cancellationToken));
}
