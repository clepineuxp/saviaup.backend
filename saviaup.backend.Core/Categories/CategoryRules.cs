using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using System.Text;

namespace SaviaUp.Backend.Core.Categories;

internal sealed record CategoryValues(
    string Name,
    string NormalizedName,
    string? Description,
    string? Image,
    bool IsInventoryTracked);

internal static class CategoryRules
{
    public static bool TryPrepare(
        string? name,
        string? description,
        string? image,
        bool isInventoryTracked,
        out CategoryValues values)
    {
        var cleanName = CleanName(name);
        var cleanDescription = CleanOptional(description);
        var cleanImage = CleanOptional(image);
        var validImage = cleanImage is null
            || cleanImage.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
            || cleanImage.StartsWith("/api/images/", StringComparison.OrdinalIgnoreCase)
            || (Uri.TryCreate(cleanImage, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
        var isValid = cleanName.Length is > 0 and <= 120
            && (cleanDescription?.Length ?? 0) <= 1000
            && validImage;

        values = new CategoryValues(
            cleanName,
            cleanName.ToUpperInvariant(),
            cleanDescription,
            cleanImage,
            isInventoryTracked);
        return isValid;
    }

    public static CategoryDto ToDto(Category category) => new(
        category.Id,
        category.Name,
        category.Description,
        category.ImageStored?.Base64Content,
        category.IsInventoryTracked,
        category.IsActive,
        category.CreatedAt,
        category.UpdatedAt);

    private static string CleanName(string? value)
        => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC).Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? CleanOptional(string? value)
    {
        var clean = value?.Trim();
        return string.IsNullOrWhiteSpace(clean) ? null : clean;
    }
}
