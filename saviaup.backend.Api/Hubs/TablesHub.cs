using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Hubs;

[Authorize]
[RequireTenant]
[RequirePermission(PermissionCodes.TablesRead)]
public sealed class TablesHub(ICurrentUserContext currentUser) : Hub
{
    public static string TenantGroup(Guid tenantId) => $"tables:tenant:{tenantId:D}";

    public override async Task OnConnectedAsync()
    {
        if (currentUser.TenantId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(currentUser.TenantId.Value));
        await base.OnConnectedAsync();
    }
}
