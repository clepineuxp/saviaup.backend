using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tenants")]
public sealed class TenantsController(
    IGetUserTenantsUseCase getUserTenantsUseCase,
    ICreateTenantUseCase createTenantUseCase,
    ISelectTenantUseCase selectTenantUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TenantDto>>> List(CancellationToken cancellationToken)
        => this.FromResult(await getUserTenantsUseCase.ExecuteAsync(currentUser.UserId!.Value, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<TenantSessionResponse>> Create(CreateTenantRequest request, CancellationToken cancellationToken)
        => this.FromResult(await createTenantUseCase.ExecuteAsync(currentUser.UserId!.Value, currentUser.SessionId!.Value, request, cancellationToken));

    [HttpPost("{tenantId:guid}/select")]
    public async Task<ActionResult<TenantSessionResponse>> Select(Guid tenantId, CancellationToken cancellationToken)
        => this.FromResult(await selectTenantUseCase.ExecuteAsync(currentUser.UserId!.Value, currentUser.SessionId!.Value, tenantId, cancellationToken));
}
