using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using System.Text;

namespace SaviaUp.Backend.Core.Categories;

internal sealed record CategoryValues(
    string Name,
    string NormalizedName,
    string? Description,
    string? ImageUrl,
    bool IsInventoryTracked);

internal static class CategoryRules
{
    public static bool TryPrepare(
        string? name,
        string? description,
        string? imageUrl,
        bool isInventoryTracked,
        out CategoryValues values)
    {
        var cleanName = CleanName(name);
        var cleanDescription = CleanOptional(description);
        var cleanImageUrl = CleanOptional(imageUrl);
        var validImageUrl = cleanImageUrl is null
            || (Uri.TryCreate(cleanImageUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
        var isValid = cleanName.Length is > 0 and <= 120
            && (cleanDescription?.Length ?? 0) <= 1000
            && (cleanImageUrl?.Length ?? 0) <= 2048
            && validImageUrl;

        values = new CategoryValues(
            cleanName,
            cleanName.ToUpperInvariant(),
            cleanDescription,
            cleanImageUrl,
            isInventoryTracked);
        return isValid;
    }

    public static CategoryDto ToDto(Category category) => new(
        category.Id,
        category.Name,
        category.Description,
        category.ImageUrl,
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
