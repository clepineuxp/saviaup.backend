using Microsoft.Extensions.Logging;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Email;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string language, string resetLink, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development password-recovery email suppressed for {EmailDomain} in language {Language}. Token and link were not logged.", EmailDomain(email), language);
        return Task.CompletedTask;
    }

    public Task SendOrganizationInvitationAsync(string email, string language, string organizationName, string invitationLink, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development organization-invitation email suppressed for {EmailDomain} in language {Language}. Link was not logged.", EmailDomain(email), language);
        return Task.CompletedTask;
    }

    private static string EmailDomain(string email)
    {
        var separator = email.LastIndexOf('@');
        return separator >= 0 ? email[separator..] : "unknown";
    }
}
