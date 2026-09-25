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
[RequirePermission(PermissionCodes.DigitalMenuAccess)]
[Route("api/digital-menu")]
public sealed class DigitalMenuController(
    IDigitalMenuUseCase digitalMenu,
    ICurrentUserContext currentUser) : ControllerBase
{
    private Guid TenantId => currentUser.TenantId!.Value;

    [HttpGet("config")]
    public async Task<ActionResult<DigitalMenuConfigDto>> GetConfig(CancellationToken cancellationToken)
        => this.FromResult(await digitalMenu.GetConfigAsync(TenantId, cancellationToken));

    [HttpPut("parameters")]
    [RequirePermission(PermissionCodes.DigitalMenuEnable)]
    public async Task<ActionResult> UpdateParameters(
        [FromBody] UpdateDigitalMenuParametersRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await digitalMenu.UpdateParametersAsync(TenantId, request, cancellationToken));

    [HttpPut("items")]
    [RequirePermission(PermissionCodes.DigitalMenuItemsManage)]
    public async Task<ActionResult> UpdateItems(
        [FromBody] SaveDigitalMenuItemsRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await digitalMenu.UpdateItemsAsync(
            TenantId,
            request,
            currentUser.UserId,
            currentUser.UserDisplayName ?? currentUser.UserEmail,
            cancellationToken));

    [HttpPut("style")]
    [RequirePermission(PermissionCodes.DigitalMenuStyleManage)]
    public async Task<ActionResult> UpdateStyle(
        [FromBody] DigitalMenuStyleDto request,
        CancellationToken cancellationToken)
        => this.FromResult(await digitalMenu.UpdateStyleAsync(TenantId, request, cancellationToken));
}
