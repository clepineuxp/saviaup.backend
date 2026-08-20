using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Authorize]
[RequireTenant]
[Route("api/modules")]
public sealed class ModulesController(
    IGetAvailableModulesUseCase useCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("available")]
    public async Task<ActionResult<AvailableModulesResponse>> Available(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ExecuteAsync(
            currentUser.UserId!.Value,
            currentUser.TenantId!.Value,
            currentUser.RoleId!.Value,
            Request.Headers.AcceptLanguage.ToString(),
            cancellationToken));
}
