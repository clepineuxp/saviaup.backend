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
    string? Image,
    decimal? SalePrice,
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
        string? image,
        decimal? salePrice,
        int? preparationTimeMinutes,
        out ProductValues values)
    {
        var cleanName = CleanWords(name);
        var cleanDescription = CleanOptional(description);
        var cleanImage = CleanOptional(image);
        var validImage = cleanImage is null
            || cleanImage.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
            || cleanImage.StartsWith("/pvc/", StringComparison.OrdinalIgnoreCase)
            || cleanImage.StartsWith("/api/images/", StringComparison.OrdinalIgnoreCase)
            || (Uri.TryCreate(cleanImage, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
        var validType = TryParseType(type, out var parsedType);

        values = new ProductValues(
            parsedType,
            cleanName,
            cleanName.ToUpperInvariant(),
            cleanDescription,
            cleanImage,
            salePrice,
            preparationTimeMinutes);
        return validType
            && cleanName.Length is > 0 and <= 120
            && (cleanDescription?.Length ?? 0) <= 1000
            && validImage
            && (salePrice is null or > 0 and <= MaximumSalePrice)
            && preparationTimeMinutes is null or >= 0;
    }

    public static bool TryValidateVariations(
        ProductType type,
        decimal? salePrice,
        IReadOnlyCollection<ProductVariationRequest>? variations,
        out decimal? effectiveSalePrice)
    {
        var hasVariations = variations is { Count: > 0 };
        effectiveSalePrice = hasVariations && type == ProductType.Normal ? null : salePrice;

        if (type == ProductType.Combo)
            return !hasVariations && salePrice is > 0 and <= MaximumSalePrice;

        if (!hasVariations)
            return salePrice is > 0 and <= MaximumSalePrice;

        var requestedVariations = variations!;
        return requestedVariations.Any(variation => variation.IsActive)
            && requestedVariations.All(variation =>
                CleanWords(variation.Name).Length is > 0 and <= 120
                && variation.SalePrice is > 0 and <= MaximumSalePrice
                && variation.Id != Guid.Empty)
            && requestedVariations
                .Select(variation => CleanWords(variation.Name).ToUpperInvariant())
                .Distinct()
                .Count() == requestedVariations.Count
            && requestedVariations
                .Where(variation => variation.Id.HasValue)
                .Select(variation => variation.Id)
                .Distinct()
                .Count() == requestedVariations.Count(variation => variation.Id.HasValue);
    }

    public static ProductDto ToDto(Product product) => new(
        product.Id,
        product.Type.ToString().ToUpperInvariant(),
        product.Name,
        product.Description,
        product.ImagePath ?? product.ImageStored?.Base64Content,
        new CategoryReferenceDto(product.Category.Id, product.Category.Name, product.Category.IsInventoryTracked),
        product.SalePrice,
        product.PreparationTimeMinutes,
        product.IsInventoryTracked,
        product.IsActive,
        product.CreatedByUserName,
        product.LastModifiedByUserName,
        product.CreatedAt,
        product.UpdatedAt,
        product.RecipeItems?
            .OrderBy(r => r.Order)
            .ThenBy(r => r.CreatedAt)
            .Select(ToRecipeDto)
            .ToArray() ?? [],
        product.Variations?
            .OrderBy(v => v.Order)
            .ThenBy(v => v.CreatedAt)
            .Select(ToVariationDto)
            .ToArray() ?? [],
        product.ComboGroups?
            .OrderBy(group => group.Order)
            .ThenBy(group => group.CreatedAt)
            .Select(ToComboGroupDto)
            .ToArray() ?? []);

    public static ProductComboGroupDto ToComboGroupDto(ProductComboGroup group) => new(
        group.Id,
        group.Name,
        group.SelectionType.ToString().ToUpperInvariant(),
        group.IsRequired,
        group.MinSelections,
        group.MaxSelections,
        group.Order,
        group.Options
            .OrderBy(option => option.Order)
            .ThenBy(option => option.CreatedAt)
            .Select(option => new ProductComboOptionDto(
                option.Id,
                option.ProductId,
                option.Product?.Name ?? string.Empty,
                option.ProductQuantity,
                option.PriceAdjustment,
                option.Order,
                option.ProductVariationId,
                option.ProductVariation?.Name))
            .ToArray());

    public static bool TryValidateComboGroups(
        ProductType type,
        IReadOnlyCollection<ProductComboGroupRequest>? groups)
    {
        if (type == ProductType.Normal) return groups is null or { Count: 0 };
        if (groups is null || groups.Count == 0) return false;

        foreach (var group in groups)
        {
            var name = CleanWords(group.Name);
            if (name.Length is 0 or > 120
                || !Enum.TryParse<ProductComboSelectionType>(group.SelectionType?.Trim(), true, out var selectionType)
                || group.Options is null
                || group.Options.Count == 0
                || group.Options.Any(option => option.ProductId == Guid.Empty || option.ProductQuantity <= 0)
                || group.Options
                    .Select(option => (option.ProductId, option.ProductVariationId))
                    .Distinct()
                    .Count() != group.Options.Count)
                return false;

            if (selectionType == ProductComboSelectionType.Fixed)
            {
                if (!group.IsRequired
                    || group.MinSelections != group.Options.Count
                    || group.MaxSelections != group.Options.Count)
                    return false;
            }
            else if (selectionType == ProductComboSelectionType.Single)
            {
                if (group.MaxSelections != 1 || group.MinSelections != (group.IsRequired ? 1 : 0))
                    return false;
            }
            else if (group.MaxSelections < 1
                || group.MinSelections < (group.IsRequired ? 1 : 0)
                || group.MinSelections > group.MaxSelections)
            {
                return false;
            }
        }

        return true;
    }

    public static List<ProductComboGroup> CreateComboGroups(
        Guid tenantId,
        Guid comboProductId,
        IReadOnlyCollection<ProductComboGroupRequest> requests,
        DateTimeOffset now)
    {
        var groups = new List<ProductComboGroup>(requests.Count);
        var groupIndex = 0;
        foreach (var request in requests)
        {
            var selectionType = Enum.Parse<ProductComboSelectionType>(request.SelectionType.Trim(), true);
            var isFixed = selectionType == ProductComboSelectionType.Fixed;
            var group = new ProductComboGroup
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ComboProductId = comboProductId,
                Name = CleanWords(request.Name),
                SelectionType = selectionType,
                IsRequired = isFixed || request.IsRequired,
                MinSelections = isFixed ? request.Options.Count : request.MinSelections,
                MaxSelections = isFixed ? request.Options.Count : request.MaxSelections,
                Order = request.Order > 0 ? request.Order : ++groupIndex,
                CreatedAt = now,
                UpdatedAt = now
            };

            var optionIndex = 0;
            foreach (var optionRequest in request.Options)
            {
                group.Options.Add(new ProductComboOption
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ComboGroupId = group.Id,
                    ProductId = optionRequest.ProductId,
                    ProductVariationId = optionRequest.ProductVariationId,
                    ProductQuantity = optionRequest.ProductQuantity,
                    PriceAdjustment = optionRequest.PriceAdjustment,
                    Order = optionRequest.Order > 0 ? optionRequest.Order : ++optionIndex,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            groups.Add(group);
        }
        return groups;
    }

    public static ProductVariationDto ToVariationDto(ProductVariation item) => new(
        item.Id,
        item.Name,
        item.SalePrice,
        item.Order,
        item.IsActive);

    public static ProductRecipeItemDto ToRecipeDto(ProductRecipeItem item) => new(
        item.Id,
        item.IngredientId,
        item.Ingredient?.Name ?? item.CustomIngredientName,
        item.Ingredient?.MeasurementUnit?.Name,
        item.Ingredient?.MeasurementUnit?.Code,
        item.CustomIngredientName,
        item.Quantity,
        item.Notes,
        item.Order,
        item.IngredientId.HasValue);

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
