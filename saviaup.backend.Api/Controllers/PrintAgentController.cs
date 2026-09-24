using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Authentication;
using SaviaUp.Backend.Api.Configuration;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/printing/agent")]
public sealed class PrintAgentController(
    IPrintAgentUseCase useCase,
    IPrintAgentContext context,
    INetworkFingerprintService networkFingerprint) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("discovery")]
    [HttpPost("discovery")]
    public async Task<ActionResult<RegisterPrintAgentDiscoveryResponse>> RegisterDiscovery(
        [FromBody] DiscoverPrintAgentRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await useCase.RegisterDiscoveryAsync(
            request,
            networkFingerprint.Compute(ClientNetworkAddress.From(HttpContext)),
            cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("discovery")]
    [HttpPost("discovery/{discoveryId:guid}/poll")]
    public async Task<ActionResult<PollPrintAgentDiscoveryResponse>> PollDiscovery(
        Guid discoveryId,
        [FromBody] PollPrintAgentDiscoveryRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await useCase.PollDiscoveryAsync(
            discoveryId,
            request,
            networkFingerprint.Compute(ClientNetworkAddress.From(HttpContext)),
            cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("discovery")]
    [HttpPost("discovery/{discoveryId:guid}/acknowledge")]
    public async Task<ActionResult> AcknowledgeDiscovery(
        Guid discoveryId,
        [FromBody] PollPrintAgentDiscoveryRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await useCase.AcknowledgeDiscoveryAsync(
            discoveryId,
            request,
            networkFingerprint.Compute(ClientNetworkAddress.From(HttpContext)),
            cancellationToken));

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("pair")]
    public async Task<ActionResult<PairPrintAgentResponse>> Pair([FromBody] PairPrintAgentRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.PairAsync(request, cancellationToken));

    [Authorize(AuthenticationSchemes = PrintAgentAuthenticationDefaults.Scheme)]
    [RequirePrintAgent]
    [HttpPost("heartbeat")]
    public async Task<ActionResult> Heartbeat([FromBody] PrintAgentHeartbeatRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.HeartbeatAsync(context.TenantId!.Value, context.AgentId!.Value, request, cancellationToken));

    [Authorize(AuthenticationSchemes = PrintAgentAuthenticationDefaults.Scheme)]
    [RequirePrintAgent]
    [HttpGet("jobs/pending")]
    public async Task<ActionResult<IReadOnlyCollection<PrintJobDto>>> PendingJobs([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
        => this.FromResult(await useCase.GetPendingJobsAsync(context.TenantId!.Value, context.AgentId!.Value, limit, cancellationToken));

    [Authorize(AuthenticationSchemes = PrintAgentAuthenticationDefaults.Scheme)]
    [RequirePrintAgent]
    [HttpPost("printers/sync")]
    public async Task<ActionResult<IReadOnlyCollection<AvailablePrinterDto>>> SyncPrinters(
        [FromBody] SyncDiscoveredPrintersRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await useCase.SyncPrintersAsync(
            context.TenantId!.Value, context.AgentId!.Value, request, cancellationToken));

    [Authorize(AuthenticationSchemes = PrintAgentAuthenticationDefaults.Scheme)]
    [RequirePrintAgent]
    [HttpPost("jobs/{jobId:guid}/status")]
    public async Task<ActionResult<PrintJobDto>> UpdateJobStatus(Guid jobId, [FromBody] PrintJobStatusRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.UpdateJobStatusAsync(
            context.TenantId!.Value, context.AgentId!.Value, jobId, request, cancellationToken));
}
