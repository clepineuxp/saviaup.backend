namespace SaviaUp.Backend.Domain.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "storage";
    public string PublicBaseUrl { get; set; } = "http://localhost:5000/pvc";
    public int WebpQuality { get; set; } = 82;
    public int MaximumWidth { get; set; } = 1920;
    public int MaximumHeight { get; set; } = 1920;
    public int CacheDurationDays { get; set; } = 365;
}
