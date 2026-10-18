using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Storage;

public sealed class LocalFileStorage(IOptions<FileStorageOptions> configuredOptions) : IFileStorage
{
    private readonly FileStorageOptions options = configuredOptions.Value;

    public async Task<StoredFileReference> SaveImageAsync(
        Guid tenantId,
        string scope,
        string entityId,
        byte[] content,
        string contentType,
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("A tenant is required.", nameof(tenantId));
        if (content.Length == 0) throw new InvalidDataException("The image is empty.");

        var safeScope = SafeSegment(scope, "general");
        var safeEntity = SafeSegment(entityId, "unassigned");
        var storedName = $"{Guid.NewGuid():N}.webp";
        var storageKey = $"{tenantId:D}/{safeScope}/{safeEntity}/{storedName}";
        var reference = $"/pvc/{storageKey}";
        var fullPath = ResolvePath(storageKey);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        byte[] encoded;
        cancellationToken.ThrowIfCancellationRequested();
        encoded = ImageProcessor.ConvertToWebp(
            content,
            options.MaximumWidth,
            options.MaximumHeight,
            options.WebpQuality);

        var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temporaryPath, encoded, cancellationToken);
        File.Move(temporaryPath, fullPath);

        return new StoredFileReference(reference, "image/webp", Path.ChangeExtension(
            Path.GetFileNameWithoutExtension(fileName ?? "image"), ".webp"), encoded.LongLength);
    }

    public async Task<StoredFileContent?> GetAsync(
        Guid tenantId,
        string reference,
        CancellationToken cancellationToken)
    {
        var fullPath = ResolveTenantPath(tenantId, reference);
        if (!File.Exists(fullPath)) return null;

        var content = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        return new StoredFileContent(
            content,
            "image/webp",
            Path.GetFileName(fullPath),
            File.GetLastWriteTimeUtc(fullPath));
    }

    public Task DeleteAsync(Guid tenantId, string? reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(reference)) return Task.CompletedTask;
        var fullPath = ResolveTenantPath(tenantId, reference);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string? GetPublicUrl(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        if (Uri.TryCreate(reference.Trim(), UriKind.Absolute, out var absoluteUri)
            && !absoluteUri.AbsolutePath.Contains("/pvc/", StringComparison.OrdinalIgnoreCase))
            return reference.Trim();
        var normalizedReference = NormalizeReference(reference);
        return $"{options.PublicBaseUrl.TrimEnd('/')}/{normalizedReference}";
    }

    private string ResolvePath(string reference)
    {
        var normalizedReference = NormalizeReference(reference);
        var root = Path.GetFullPath(options.RootPath);
        var fullPath = Path.GetFullPath(Path.Combine(
            root,
            normalizedReference.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The file reference escapes the configured storage root.");
        return fullPath;
    }

    private string ResolveTenantPath(Guid tenantId, string reference)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("A tenant is required.", nameof(tenantId));
        var normalizedReference = NormalizeReference(reference);
        var tenantPrefix = $"{tenantId:D}/";
        if (!normalizedReference.StartsWith(tenantPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The file reference does not belong to the requested tenant.");
        return ResolvePath(normalizedReference);
    }

    private static string NormalizeReference(string reference)
    {
        var value = reference.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) value = uri.AbsolutePath;
        var pvcIndex = value.IndexOf("/pvc/", StringComparison.OrdinalIgnoreCase);
        if (pvcIndex >= 0) value = value[(pvcIndex + 5)..];
        return value.Trim('/').Replace('\\', '/');
    }

    private static string SafeSegment(string? value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var safe = new string(source
            .Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'
                ? char.ToLowerInvariant(character)
                : '-')
            .ToArray()).Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }
}
