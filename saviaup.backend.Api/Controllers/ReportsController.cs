using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
[RequireTenant]
public sealed class ReportsController(
    IReportsUseCase reportsUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpPost]
    [RequirePermission([PermissionCodes.ReportsRead, PermissionCodes.ReportsManage])]
    public async Task<ActionResult<GeneratedReportDto>> RequestReport(
        [FromBody] CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId!.Value;
        var userId = currentUser.UserId ?? Guid.Empty;
        var userName = currentUser.UserDisplayName ?? currentUser.UserEmail ?? "Usuario";

        var result = await reportsUseCase.RequestReportAsync(
            tenantId,
            userId,
            userName,
            request,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.Error(result.Error!);
        }

        return AcceptedAtAction(
            nameof(GetReportStatus),
            new { id = result.Value!.Id },
            result.Value);
    }

    [HttpGet]
    [RequirePermission([PermissionCodes.ReportsRead, PermissionCodes.ReportsManage])]
    public async Task<ActionResult<IReadOnlyCollection<GeneratedReportDto>>> ListReports(
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = currentUser.TenantId!.Value;
            var result = await reportsUseCase.ListReportsAsync(tenantId, cancellationToken);
            return this.FromResult(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
    }

    [HttpGet("{id:guid}")]
    [RequirePermission([PermissionCodes.ReportsRead, PermissionCodes.ReportsManage])]
    public async Task<ActionResult<GeneratedReportDto>> GetReportStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = currentUser.TenantId!.Value;
            var result = await reportsUseCase.GetReportStatusAsync(tenantId, id, cancellationToken);
            return this.FromResult(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
    }

    [HttpGet("{id:guid}/download")]
    [RequirePermission([PermissionCodes.ReportsRead, PermissionCodes.ReportsManage])]
    public async Task<IActionResult> DownloadReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId!.Value;
        var result = await reportsUseCase.DownloadReportAsync(tenantId, id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.Error(result.Error!);
        }

        return File(
            result.Value!.Content,
            result.Value.ContentType,
            result.Value.FileName);
    }
}
