using System.Security.Claims;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Configuration;

public sealed class PrintAgentContext(IHttpContextAccessor accessor) : IPrintAgentContext
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true && AgentId.HasValue;
    public Guid? AgentId => ReadGuid(ClaimNames.PrintAgentId);
    public Guid? TenantId => ReadGuid(ClaimNames.TenantId);
    public Guid? LocationId => ReadGuid(ClaimNames.LocationId);

    private Guid? ReadGuid(string name)
        => Guid.TryParse(User?.FindFirst(name)?.Value, out var value) ? value : null;
}
