using System.IdentityModel.Tokens.Jwt;
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
[Route("api/suppliers")]
public sealed class SuppliersController(
    IGetSuppliersUseCase getPageUseCase,
    IGetSupplierLookupUseCase getLookupUseCase,
    ICreateSupplierUseCase createUseCase,
    IUpdateSupplierUseCase updateUseCase,
    ISetSupplierStatusUseCase setStatusUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.SuppliersRead)]
    public async Task<ActionResult<SupplierPageDto>> GetPage(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => this.FromResult(await getPageUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            search,
            isActive,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("lookup")]
    [RequirePermission(PermissionCodes.SuppliersRead, PermissionCodes.ExpensesRead, PermissionCodes.ExpensesCreate, PermissionCodes.ExpensesEdit)]
    public async Task<ActionResult<IReadOnlyCollection<SupplierLookupDto>>> GetLookup(
        CancellationToken cancellationToken = default)
        => this.FromResult(await getLookupUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.SuppliersManage)]
    public async Task<ActionResult<SupplierDto>> Create(
        CreateSupplierRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    [HttpPut("{supplierId:guid}")]
    [RequirePermission(PermissionCodes.SuppliersManage)]
    public async Task<ActionResult<SupplierDto>> Update(
        Guid supplierId,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            supplierId,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    [HttpPatch("{supplierId:guid}/status")]
    [RequirePermission(PermissionCodes.SuppliersManage)]
    public async Task<ActionResult<SupplierDto>> SetStatus(
        Guid supplierId,
        SetSupplierStatusRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await setStatusUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            supplierId,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    private string GetUserName() =>
        currentUser.UserEmail ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? currentUser.UserId?.ToString() ?? "Usuario";
}
