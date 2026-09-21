using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/printing")]
[Authorize]
[RequireTenant]
public sealed class PrintingController(
    IPrintingAdministrationUseCase useCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("configuration")]
    [RequirePermission(PermissionCodes.PrintingAgentsRead, PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<PrintingConfigurationDto>> GetConfiguration(CancellationToken cancellationToken)
        => this.FromResult(await useCase.GetConfigurationAsync(cancellationToken));

    [HttpGet("routing-options")]
    [RequirePermission(PermissionCodes.PrintingZonesRead, PermissionCodes.PrintingZonesManage)]
    public async Task<ActionResult<PrintingRoutingOptionsDto>> GetRoutingOptions(CancellationToken cancellationToken)
        => this.FromResult(await useCase.GetRoutingOptionsAsync(currentUser.TenantId!.Value, cancellationToken));

    [HttpGet("locations")]
    [RequirePermission(PermissionCodes.PrintingAgentsRead, PermissionCodes.PrintingAgentsManage,
        PermissionCodes.PrintingZonesRead, PermissionCodes.PrintingZonesManage)]
    public async Task<ActionResult<IReadOnlyCollection<PrintingLocationDto>>> GetLocations(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListLocationsAsync(currentUser.TenantId!.Value, cancellationToken));

    [HttpPost("agents/pairing-codes")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<PairingCodeDto>> CreatePairingCode(
        [FromBody] CreatePairingCodeRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await useCase.CreatePairingCodeAsync(
            currentUser.TenantId!.Value, currentUser.UserId!.Value, request, cancellationToken));

    [HttpGet("agents/discovered")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<IReadOnlyCollection<DiscoveredPrintAgentDto>>> GetDiscoveredAgents(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListDiscoveredAgentsAsync(cancellationToken));

    [HttpPost("agents/link-discovered")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<PrintAgentDto>> LinkDiscoveredAgent(
        [FromBody] LinkDiscoveredPrintAgentRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await useCase.LinkDiscoveredAgentAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpGet("agents")]
    [RequirePermission(PermissionCodes.PrintingAgentsRead, PermissionCodes.PrintingAgentsManage, PermissionCodes.PrintingZonesRead,
        PermissionCodes.PrintingZonesManage, PermissionCodes.PrintingQueueRead)]
    public async Task<ActionResult<IReadOnlyCollection<PrintAgentDto>>> GetAgents(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListAgentsAsync(currentUser.TenantId!.Value, cancellationToken));

    [HttpGet("agents/{agentId:guid}")]
    [RequirePermission(PermissionCodes.PrintingAgentsRead, PermissionCodes.PrintingAgentsManage, PermissionCodes.PrintingZonesRead,
        PermissionCodes.PrintingZonesManage, PermissionCodes.PrintingQueueRead)]
    public async Task<ActionResult<PrintAgentDto>> GetAgent(Guid agentId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.GetAgentAsync(currentUser.TenantId!.Value, agentId, cancellationToken));

    [HttpPut("agents/{agentId:guid}")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<PrintAgentDto>> UpdateAgent(Guid agentId, [FromBody] UpdatePrintAgentRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.UpdateAgentAsync(currentUser.TenantId!.Value, agentId, request, cancellationToken));

    [HttpDelete("agents/{agentId:guid}")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult> DeleteAgent(Guid agentId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.DeleteAgentAsync(currentUser.TenantId!.Value, agentId, cancellationToken));

    [HttpGet("agents/{agentId:guid}/printers")]
    [RequirePermission(PermissionCodes.PrintingAgentsRead, PermissionCodes.PrintingAgentsManage, PermissionCodes.PrintingZonesRead,
        PermissionCodes.PrintingZonesManage, PermissionCodes.PrintingQueueRead)]
    public async Task<ActionResult<IReadOnlyCollection<PrinterDto>>> GetAgentPrinters(Guid agentId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListPrintersAsync(currentUser.TenantId!.Value, agentId, cancellationToken));

    [HttpGet("agents/{agentId:guid}/available-printers")]
    [RequirePermission(PermissionCodes.PrintingAgentsRead, PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<IReadOnlyCollection<AvailablePrinterDto>>> GetAvailablePrinters(
        Guid agentId,
        CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListAvailablePrintersAsync(
            currentUser.TenantId!.Value, agentId, cancellationToken));

    [HttpPost("agents/{agentId:guid}/discover-printers")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult> DiscoverPrinters(Guid agentId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.RequestPrinterDiscoveryAsync(
            currentUser.TenantId!.Value, agentId, cancellationToken));

    [HttpGet("printers")]
    [RequirePermission(PermissionCodes.PrintingAgentsRead, PermissionCodes.PrintingAgentsManage, PermissionCodes.PrintingZonesRead,
        PermissionCodes.PrintingZonesManage, PermissionCodes.PrintingQueueRead)]
    public async Task<ActionResult<IReadOnlyCollection<PrinterDto>>> GetPrinters(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListPrintersAsync(currentUser.TenantId!.Value, null, cancellationToken));

    [HttpPost("agents/{agentId:guid}/printers")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<PrinterDto>> CreatePrinter(Guid agentId, [FromBody] SavePrinterRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.CreatePrinterAsync(currentUser.TenantId!.Value, request with { PrintAgentId = agentId }, cancellationToken));

    [HttpPut("printers/{printerId:guid}")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<PrinterDto>> UpdatePrinter(Guid printerId, [FromBody] SavePrinterRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.UpdatePrinterAsync(currentUser.TenantId!.Value, printerId, request, cancellationToken));

    [HttpDelete("printers/{printerId:guid}")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult> DeletePrinter(Guid printerId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.DeletePrinterAsync(currentUser.TenantId!.Value, printerId, cancellationToken));

    [HttpPost("agents/{agentId:guid}/test-print")]
    [RequirePermission(PermissionCodes.PrintingAgentsManage)]
    public async Task<ActionResult<PrintJobDto>> TestPrint(Guid agentId, [FromQuery] Guid printerId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.CreateTestJobAsync(
            currentUser.TenantId!.Value, agentId, printerId, currentUser.UserId!.Value, cancellationToken));

    [HttpGet("zones")]
    [RequirePermission(PermissionCodes.PrintingZonesRead, PermissionCodes.PrintingZonesManage)]
    public async Task<ActionResult<IReadOnlyCollection<PrintingZoneDto>>> GetZones(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListZonesAsync(currentUser.TenantId!.Value, cancellationToken));

    [HttpPost("zones")]
    [RequirePermission(PermissionCodes.PrintingZonesManage)]
    public async Task<ActionResult<PrintingZoneDto>> CreateZone([FromBody] SavePrintingZoneRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.CreateZoneAsync(currentUser.TenantId!.Value, request, cancellationToken));

    [HttpPut("zones/{zoneId:guid}")]
    [RequirePermission(PermissionCodes.PrintingZonesManage)]
    public async Task<ActionResult<PrintingZoneDto>> UpdateZone(Guid zoneId, [FromBody] SavePrintingZoneRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.UpdateZoneAsync(currentUser.TenantId!.Value, zoneId, request, cancellationToken));

    [HttpDelete("zones/{zoneId:guid}")]
    [RequirePermission(PermissionCodes.PrintingZonesManage)]
    public async Task<ActionResult> DeleteZone(Guid zoneId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.DeleteZoneAsync(currentUser.TenantId!.Value, zoneId, cancellationToken));

    [HttpGet("jobs")]
    [RequirePermission(PermissionCodes.PrintingQueueRead)]
    public async Task<ActionResult<PrintJobPageDto>> GetJobs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null,
        [FromQuery] string? status = null, [FromQuery] Guid? zoneId = null, [FromQuery] Guid? agentId = null,
        [FromQuery] Guid? printerId = null, [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null, CancellationToken cancellationToken = default)
        => this.FromResult(await useCase.ListJobsAsync(currentUser.TenantId!.Value,
            new PrintJobQueryRequest(page, pageSize, search, status, zoneId, agentId, printerId,
                FromLocalDate: fromDate, ToLocalDate: toDate), cancellationToken));

    [HttpGet("jobs/{jobId:guid}")]
    [RequirePermission(PermissionCodes.PrintingQueueRead)]
    public async Task<ActionResult<PrintJobDto>> GetJob(Guid jobId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.GetJobAsync(currentUser.TenantId!.Value, jobId, cancellationToken));

    [HttpPost("jobs/{jobId:guid}/cancel")]
    [RequirePermission(PermissionCodes.PrintingQueueRetry)]
    public async Task<ActionResult<PrintJobDto>> CancelJob(Guid jobId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.CancelJobAsync(currentUser.TenantId!.Value, jobId, cancellationToken));

    [HttpPost("jobs/{jobId:guid}/retry")]
    [RequirePermission(PermissionCodes.PrintingQueueRetry)]
    public async Task<ActionResult<PrintJobDto>> RetryJob(Guid jobId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.RetryJobAsync(currentUser.TenantId!.Value, jobId, cancellationToken));

    [HttpPost("jobs/{jobId:guid}/reprint")]
    [RequirePermission(PermissionCodes.PrintingQueueReprint)]
    public async Task<ActionResult<PrintJobDto>> ReprintJob(Guid jobId, CancellationToken cancellationToken)
        => this.FromResult(await useCase.ReprintJobAsync(
            currentUser.TenantId!.Value, jobId, currentUser.UserId!.Value, cancellationToken));
}
