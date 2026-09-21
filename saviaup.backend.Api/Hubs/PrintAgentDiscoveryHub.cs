using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Hubs;

// This hub deliberately has no device credential: it is used only while an
// installed agent is waiting to be linked.  It exposes no tenant data or jobs.
public sealed class PrintAgentDiscoveryHub(IUnpairedPrintAgentRegistry registry) : Hub
{
    public Task Register(DiscoverPrintAgentRequest request)
        => registry.RegisterAsync(Context.ConnectionId, request, Context.ConnectionAborted);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await registry.UnregisterAsync(Context.ConnectionId, CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }
}

public sealed class UnpairedPrintAgentRegistry(IHubContext<PrintAgentDiscoveryHub> hub)
    : IUnpairedPrintAgentRegistry
{
    private sealed record Entry(DiscoveredPrintAgentDto Agent, string ConnectionId);
    private readonly ConcurrentDictionary<Guid, Entry> entries = new();

    public Task RegisterAsync(string connectionId, DiscoverPrintAgentRequest request, CancellationToken cancellationToken)
    {
        var previous = entries.Where(item => item.Value.ConnectionId == connectionId ||
            string.Equals(item.Value.Agent.DeviceIdentifier, request.DeviceIdentifier.Trim(), StringComparison.Ordinal))
            .Select(item => item.Key)
            .ToArray();
        foreach (var id in previous) entries.TryRemove(id, out _);

        var agent = new DiscoveredPrintAgentDto(
            Guid.NewGuid(), request.DeviceIdentifier.Trim(), request.Hostname.Trim(),
            request.OperatingSystem.Trim(), request.Version.Trim(),
            string.IsNullOrWhiteSpace(request.LocalIpAddress) ? null : request.LocalIpAddress.Trim(),
            DateTimeOffset.UtcNow);
        entries[agent.DiscoveryId] = new Entry(agent, connectionId);
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        foreach (var entry in entries.Where(item => item.Value.ConnectionId == connectionId).ToArray())
            entries.TryRemove(entry.Key, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DiscoveredPrintAgentDto>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<DiscoveredPrintAgentDto>>(
            entries.Values.Select(entry => entry.Agent).OrderBy(entry => entry.Hostname).ToArray());

    public Task<DiscoveredPrintAgentDto?> FindAsync(Guid discoveryId, CancellationToken cancellationToken)
        => Task.FromResult(entries.TryGetValue(discoveryId, out var entry) ? entry.Agent : null);

    public async Task<bool> DeliverPairingAsync(
        Guid discoveryId,
        PairPrintAgentResponse response,
        CancellationToken cancellationToken)
    {
        if (!entries.TryGetValue(discoveryId, out var entry)) return false;
        try
        {
            await hub.Clients.Client(entry.ConnectionId).SendAsync("OnAgentPaired", response, cancellationToken);
            entries.TryRemove(discoveryId, out _);
            return true;
        }
        catch (HubException)
        {
            entries.TryRemove(discoveryId, out _);
            return false;
        }
    }
}
