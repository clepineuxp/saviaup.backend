using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Authentication;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Hubs;

[Authorize(AuthenticationSchemes = PrintAgentAuthenticationDefaults.Scheme)]
[RequirePrintAgent]
public sealed class PrintingHub(IPrintAgentContext agentContext) : Hub
{
    public static string AgentGroup(Guid agentId) => $"printing:agent:{agentId:D}";

    public override async Task OnConnectedAsync()
    {
        if (agentContext.AgentId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, AgentGroup(agentContext.AgentId.Value));
        await base.OnConnectedAsync();
    }
}

public sealed class PrintingRealtimeNotifier(IHubContext<PrintingHub> hub) : IPrintingRealtimeNotifier
{
    public Task JobAvailableAsync(Guid agentId, Guid printJobId, CancellationToken cancellationToken)
        => hub.Clients.Group(PrintingHub.AgentGroup(agentId))
            .SendAsync("OnPrintJobAvailable", new { printJobId }, cancellationToken);

    public Task PrinterDiscoveryRequestedAsync(Guid agentId, CancellationToken cancellationToken)
        => hub.Clients.Group(PrintingHub.AgentGroup(agentId))
            .SendAsync("OnPrinterDiscoveryRequested", cancellationToken);
}
