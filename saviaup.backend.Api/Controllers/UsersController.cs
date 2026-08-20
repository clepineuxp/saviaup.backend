using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IGetCurrentUserUseCase useCase, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ExecuteAsync(currentUser.UserId!.Value, currentUser.TenantId, currentUser.RoleId, cancellationToken));
}
