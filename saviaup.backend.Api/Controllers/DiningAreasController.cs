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
[RequirePermission(PermissionCodes.TablesManage)]
[Route("api/table-areas")]
public sealed class DiningAreasController(
    IListDiningAreasUseCase listUseCase,
    ICreateDiningAreaUseCase createUseCase,
    IUpdateDiningAreaUseCase updateUseCase,
    IReorderDiningAreasUseCase reorderUseCase,
    IDeleteDiningAreaUseCase deleteUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DiningAreaDto>>> List(CancellationToken cancellationToken)
        => this.FromResult(await listUseCase.ExecuteAsync(currentUser.TenantId!.Value, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<DiningAreaDto>> Create(
        CreateDiningAreaRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPut("{areaId:guid}")]
    public async Task<ActionResult<DiningAreaDto>> Update(
        Guid areaId,
        UpdateDiningAreaRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(currentUser.TenantId!.Value, areaId, request, cancellationToken));

    [HttpPut("reorder")]
    public async Task<ActionResult<IReadOnlyCollection<DiningAreaDto>>> Reorder(
        ReorderDiningAreasRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await reorderUseCase.ExecuteAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpDelete("{areaId:guid}")]
    public async Task<ActionResult> Delete(Guid areaId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(currentUser.TenantId!.Value, areaId, cancellationToken));
}
