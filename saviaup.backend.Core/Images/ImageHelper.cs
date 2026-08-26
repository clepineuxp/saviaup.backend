using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Core.Images;

public static class ImageHelper
{
    public static StoredImage? CreateStoredImage(Guid tenantId, string module, string entityId, string base64DataUrl, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(base64DataUrl)) return null;

        string rawBase64 = base64DataUrl;
        string contentType = "image/png";

        if (rawBase64.Contains(";base64,"))
        {
            var parts = rawBase64.Split(";base64,");
            if (parts.Length == 2)
            {
                contentType = parts[0].Replace("data:", "").Trim();
                rawBase64 = parts[1];
            }
        }

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(rawBase64);
        }
        catch
        {
            return null;
        }

        var imageId = Guid.NewGuid();
        return new StoredImage
        {
            Id = imageId,
            TenantId = tenantId,
            Module = module.ToLowerInvariant().Trim(),
            EntityId = entityId,
            FileName = $"{module}_{entityId}_{now.Ticks}.png",
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType,
            Base64Content = base64DataUrl,
            FileSize = imageBytes.Length,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
