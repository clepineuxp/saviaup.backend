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
[Route("api/expenses")]
public sealed class ExpensesController(
    IGetExpensesUseCase getPageUseCase,
    ICreateExpenseUseCase createUseCase,
    IUpdateExpenseUseCase updateUseCase,
    IAnnulExpenseUseCase annulUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.ExpensesRead)]
    public async Task<ActionResult<ExpensePageDto>> GetPage(
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] string? search,
        [FromQuery] Guid? supplierId,
        [FromQuery] string? status,
        [FromQuery] string? paymentMethod,
        [FromQuery] bool? isCashOut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => this.FromResult(await getPageUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            fromDate,
            toDate,
            search,
            supplierId,
            status,
            paymentMethod,
            isCashOut,
            page,
            pageSize,
            cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.ExpensesCreate)]
    public async Task<ActionResult<ExpenseDto>> Create(
        CreateExpenseRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    [HttpPut("{expenseId:guid}")]
    [RequirePermission(PermissionCodes.ExpensesEdit)]
    public async Task<ActionResult<ExpenseDto>> Update(
        Guid expenseId,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            expenseId,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    [HttpPost("{expenseId:guid}/annul")]
    [RequirePermission(PermissionCodes.ExpensesAnnul)]
    public async Task<ActionResult<ExpenseDto>> Annul(
        Guid expenseId,
        AnnulExpenseRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await annulUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            expenseId,
            currentUser.UserId!.Value,
            GetUserName(),
            request,
            cancellationToken));

    private string GetUserName() =>
        currentUser.UserEmail ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? currentUser.UserId?.ToString() ?? "Usuario";
}
