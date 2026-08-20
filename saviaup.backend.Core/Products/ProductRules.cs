using System.Text;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Products;

internal sealed record ProductValues(
    ProductType Type,
    string Name,
    string NormalizedName,
    string? Description,
    string? ImageUrl,
    decimal SalePrice,
    int? PreparationTimeMinutes);

internal static class ProductRules
{
    public const decimal MaximumSalePrice = 9999999999999999.99m;

    public static bool TryParseType(string? value, out ProductType type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            type = ProductType.Normal;
            return true;
        }

        return Enum.TryParse(value.Trim(), true, out type)
            && type is ProductType.Normal or ProductType.Combo;
    }

    public static bool TryPrepare(
        string? type,
        string? name,
        string? description,
        string? imageUrl,
        decimal salePrice,
        int? preparationTimeMinutes,
        out ProductValues values)
    {
        var cleanName = CleanWords(name);
        var cleanDescription = CleanOptional(description);
        var cleanImageUrl = CleanOptional(imageUrl);
        var validImageUrl = cleanImageUrl is null
            || (Uri.TryCreate(cleanImageUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
        var validType = TryParseType(type, out var parsedType);

        values = new ProductValues(
            parsedType,
            cleanName,
            cleanName.ToUpperInvariant(),
            cleanDescription,
            cleanImageUrl,
            salePrice,
            preparationTimeMinutes);
        return validType
            && cleanName.Length is > 0 and <= 120
            && (cleanDescription?.Length ?? 0) <= 1000
            && (cleanImageUrl?.Length ?? 0) <= 2048
            && validImageUrl
            && salePrice is > 0 and <= MaximumSalePrice
            && preparationTimeMinutes is null or >= 0;
    }

    public static ProductDto ToDto(Product product) => new(
        product.Id,
        product.Type.ToString().ToUpperInvariant(),
        product.Name,
        product.Description,
        product.ImageUrl,
        new CategoryReferenceDto(product.Category.Id, product.Category.Name, product.Category.IsInventoryTracked),
        product.SalePrice,
        product.PreparationTimeMinutes,
        product.IsInventoryTracked,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt);

    public static PagedResponse<ProductDto> ToPage(PageData<Product> page, int pageNumber, int pageSize)
        => new(
            page.Items.Select(ToDto).ToArray(),
            pageNumber,
            pageSize,
            page.TotalCount,
            page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)pageSize));

    private static string CleanWords(string? value)
        => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC).Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? CleanOptional(string? value)
    {
        var clean = value?.Trim();
        return string.IsNullOrWhiteSpace(clean) ? null : clean;
    }
}
