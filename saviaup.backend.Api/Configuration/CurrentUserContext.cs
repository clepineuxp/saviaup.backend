using System.IdentityModel.Tokens.Jwt;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Configuration;

public sealed class CurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    private System.Security.Claims.ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public Guid? UserId => ReadGuid(JwtRegisteredClaimNames.Sub);
    public Guid? SessionId => ReadGuid(ClaimNames.SessionId);
    public Guid? TenantId => ReadGuid(ClaimNames.TenantId);
    public Guid? RoleId => ReadGuid(ClaimNames.RoleId);

    private Guid? ReadGuid(string name)
        => Guid.TryParse(User?.FindFirst(name)?.Value, out var value) ? value : null;
}
