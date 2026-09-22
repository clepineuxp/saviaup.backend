using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Api.Middleware;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Api.Authentication;

public static class PrintAgentAuthenticationDefaults
{
    public const string Scheme = "PrintAgent";
}

public sealed class PrintAgentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IPrintingRepository repository,
    ITokenGenerator tokenGenerator,
    IDateTimeProvider clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token)) return AuthenticateResult.NoResult();
        var agent = await repository.AuthenticateAgentAsync(tokenGenerator.Hash(token), clock.UtcNow, Context.RequestAborted);
        if (agent is null || !agent.Enabled) return AuthenticateResult.Fail("Invalid print agent credential.");

        var claims = new[]
        {
            new Claim(ClaimNames.PrintAgentId, agent.AgentId.ToString("D")),
            new Claim(ClaimNames.TenantId, agent.TenantId.ToString("D")),
            new Claim(ClaimNames.LocationId, agent.LocationId.ToString("D"))
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted) return Task.CompletedTask;
        return Context.WriteErrorAsync(
            StatusCodes.Status401Unauthorized,
            ErrorCodes.PrintAgentCredentialInvalid,
            TranslationCatalog.Translate(
                LocalizationKeys.PrintAgentCredentialInvalid,
                Request.Headers.AcceptLanguage.ToString()));
    }

    private string? ReadToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization[7..].Trim();
        if (Request.Path.StartsWithSegments("/hubs/printing"))
        {
            var queryToken = Request.Query["access_token"].ToString();
            if (!string.IsNullOrWhiteSpace(queryToken)) return queryToken;
        }
        return null;
    }
}
