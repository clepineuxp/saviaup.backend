using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Core.Images;

public static class ImageHelper
{
    private const int MaximumImageBytes = 2 * 1024 * 1024;

    public static async Task<StoredFileReference?> SaveDataUrlAsync(
        IFileStorage fileStorage,
        Guid tenantId,
        string scope,
        string entityId,
        string base64DataUrl,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeDataUrl(base64DataUrl, out var content, out var contentType)) return null;
        return await fileStorage.SaveImageAsync(
            tenantId,
            scope,
            entityId,
            content,
            contentType,
            $"{scope}-{entityId}.webp",
            cancellationToken);
    }

    public static bool IsManagedReference(string? reference)
        => !string.IsNullOrWhiteSpace(reference)
            && (!Uri.TryCreate(reference, UriKind.Absolute, out _)
                || reference.Contains("/pvc/", StringComparison.OrdinalIgnoreCase));

    public static bool IsReferenceOwnedByTenant(string? reference, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(reference)
            || !reference.Contains("/pvc/", StringComparison.OrdinalIgnoreCase))
            return true;

        var path = Uri.TryCreate(reference, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath
            : reference.Trim();
        return path.StartsWith($"/pvc/{tenantId:D}/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDecodeDataUrl(
        string dataUrl,
        out byte[] content,
        out string contentType)
    {
        content = [];
        contentType = "image/png";
        if (string.IsNullOrWhiteSpace(dataUrl)) return false;

        var rawBase64 = dataUrl.Trim();
        var separatorIndex = rawBase64.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (separatorIndex >= 0)
        {
            contentType = rawBase64[5..separatorIndex].Trim();
            rawBase64 = rawBase64[(separatorIndex + 8)..];
        }

        try
        {
            content = Convert.FromBase64String(rawBase64);
            return content.Length is > 0 and <= MaximumImageBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
