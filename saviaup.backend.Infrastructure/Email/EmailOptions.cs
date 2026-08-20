namespace SaviaUp.Backend.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Mode { get; set; } = "Development";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@saviaup.local";
    public string FromName { get; set; } = "Savia Up";
}
