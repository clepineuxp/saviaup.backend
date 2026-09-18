namespace SaviaUp.Backend.Domain.Options;

public sealed class AdminApiOptions
{
    public const string SectionName = "AdminApi";

    public string BaseUrl { get; set; } = "http://localhost:5100";
    public int TimeoutSeconds { get; set; } = 5;
}
