using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Infrastructure.Email;

public sealed class EmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendPasswordResetAsync(string email, string language, string resetLink, CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = TranslationCatalog.Translate(LocalizationKeys.PasswordResetSubject, language),
            Body = string.Format(TranslationCatalog.Translate(LocalizationKeys.PasswordResetBody, language), resetLink),
            IsBodyHtml = false
        };
        message.To.Add(email);
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Username, _options.Password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }

    public Task SendOrganizationInvitationAsync(string email, string language, string organizationName, string invitationLink, CancellationToken cancellationToken)
        => SendAsync(email,
            TranslationCatalog.Translate(LocalizationKeys.OrganizationInvitationSubject, language),
            string.Format(TranslationCatalog.Translate(LocalizationKeys.OrganizationInvitationBody, language), organizationName, invitationLink),
            cancellationToken);

    private async Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(email);
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Username, _options.Password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
