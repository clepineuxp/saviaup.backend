using Microsoft.AspNetCore.SignalR;
using Moq;
using SaviaUp.Backend.Api.Hubs;
using SaviaUp.Backend.Domain.DTOs;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class PrintAgentDiscoveryRegistryTests
{
    [Fact]
    public async Task Registry_ExposesAndFindsAgents_OnlyForTheSameSourceIp()
    {
        var registry = new UnpairedPrintAgentRegistry(Mock.Of<IHubContext<PrintAgentDiscoveryHub>>());
        await registry.RegisterAsync("connection-a", Request("device-a"), "203.0.113.10", CancellationToken.None);
        await registry.RegisterAsync("connection-b", Request("device-b"), "203.0.113.11", CancellationToken.None);

        var sameNetworkAgents = await registry.ListAsync("203.0.113.10", CancellationToken.None);
        var otherNetworkAgents = await registry.ListAsync("203.0.113.12", CancellationToken.None);

        var agent = Assert.Single(sameNetworkAgents);
        Assert.Equal("device-a", agent.DeviceIdentifier);
        Assert.Empty(otherNetworkAgents);
        Assert.NotNull(await registry.FindAsync(agent.DiscoveryId, "203.0.113.10", CancellationToken.None));
        Assert.Null(await registry.FindAsync(agent.DiscoveryId, "203.0.113.11", CancellationToken.None));
    }

    [Fact]
    public async Task Registry_DoesNotPublishAnAgent_WhenItsSourceIpIsUnavailable()
    {
        var registry = new UnpairedPrintAgentRegistry(Mock.Of<IHubContext<PrintAgentDiscoveryHub>>());

        await registry.RegisterAsync("connection-a", Request("device-a"), null, CancellationToken.None);

        Assert.Empty(await registry.ListAsync("203.0.113.10", CancellationToken.None));
    }

    private static DiscoverPrintAgentRequest Request(string deviceIdentifier)
        => new(deviceIdentifier, "POS-01", "Windows 11", "1.0.0", "192.168.1.10");
}
