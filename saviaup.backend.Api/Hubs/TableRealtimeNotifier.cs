using Microsoft.AspNetCore.SignalR;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Hubs;

public sealed class TableRealtimeNotifier(IHubContext<TablesHub> hub) : ITableRealtimeNotifier
{
    public Task StatusChangedAsync(
        Guid tenantId,
        TableStatusChangedEvent notification,
        CancellationToken cancellationToken)
        => hub.Clients.Group(TablesHub.TenantGroup(tenantId))
            .SendAsync("OnTableStatusChanged", notification, cancellationToken);

    public Task OrderUpdatedAsync(
        Guid tenantId,
        TableOrderUpdatedEvent notification,
        CancellationToken cancellationToken)
        => hub.Clients.Group(TablesHub.TenantGroup(tenantId))
            .SendAsync("OnTableOrderUpdated", notification, cancellationToken);
}
