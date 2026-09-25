using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class DigitalMenuRepository(
    PlatformDbContext platformContext,
    ApplicationDbContext appContext,
    IFileStorage? fileStorage = null) : IDigitalMenuRepository
{
    private const int PublicImageMaxSize = 640;
    private const int PublicImageCompactSize = 480;
    private const int PublicImageThumbnailSize = 360;
    private const int PublicImageTargetBytes = 64 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyCollection<DigitalMenuItem>> GetItemsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await appContext.DigitalMenuItems
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.SortOrder)
            .ToArrayAsync(cancellationToken);
    }

    public async Task ReplaceItemsAsync(Guid tenantId, IEnumerable<DigitalMenuItem> items, CancellationToken cancellationToken)
    {
        var existing = await appContext.DigitalMenuItems
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            appContext.DigitalMenuItems.RemoveRange(existing);
        }

        await appContext.DigitalMenuItems.AddRangeAsync(items, cancellationToken);
    }

    public async Task<PublicDigitalMenuDto?> GetPublicMenuAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        // 1. Find slug parameter across all tenants
        var slugParam = await appContext.OrganizationParameters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Key == OrganizationParameterKeys.DigitalMenuSlug && p.Value.ToLower() == normalizedSlug, cancellationToken);

        if (slugParam is null || string.IsNullOrWhiteSpace(slugParam.Value)) return null;

        var tenantId = slugParam.TenantId;

        // 2. Check if digital menu is enabled for this tenant
        var enabledParam = await appContext.OrganizationParameters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Key == OrganizationParameterKeys.EnableDigitalMenu, cancellationToken);

        if (enabledParam is null || !bool.TryParse(enabledParam.Value, out var isEnabled) || !isEnabled)
        {
            return null;
        }

        // 3. Ensure Tenant exists and is active
        var tenant = await platformContext.Tenants
            .AsNoTracking()
            .Where(item => item.Id == tenantId && item.IsActive)
            .Select(item => new
            {
                item.Id,
                item.Name,
                HasLogo = item.LogoPath != null || item.LogoData != null && item.LogoData.Length > 0,
                item.LogoPath,
                item.LogoData,
                item.LogoContentType,
                item.Phone,
                item.Address,
                item.Website,
                item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null) return null;

        // 4. Style configuration
        var styleParam = await appContext.OrganizationParameters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Key == OrganizationParameterKeys.DigitalMenuStyle, cancellationToken);

        var style = ParseStyle(styleParam?.Value);

        // 5. Fetch categories, products, and menu items
        var rawCategories = await appContext.Categories
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .ToListAsync(cancellationToken);

        var rawProducts = await appContext.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(product => product.ComboGroups.Where(group => group.TenantId == tenantId))
                .ThenInclude(group => group.Options.Where(option => option.TenantId == tenantId))
                    .ThenInclude(option => option.Product)
            .Include(product => product.ComboGroups.Where(group => group.TenantId == tenantId))
                .ThenInclude(group => group.Options.Where(option => option.TenantId == tenantId))
                    .ThenInclude(option => option.ProductVariation)
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync(cancellationToken);

        var variationsByProductId = (await appContext.ProductVariations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(variation => variation.TenantId == tenantId && variation.IsActive)
            .OrderBy(variation => variation.Order)
            .ThenBy(variation => variation.Name)
            .Select(variation => new
            {
                variation.ProductId,
                variation.Id,
                variation.Name,
                variation.SalePrice,
                SortOrder = variation.Order
            })
            .ToListAsync(cancellationToken))
            .GroupBy(variation => variation.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<PublicProductVariationDto>)group
                    .Select(variation => new PublicProductVariationDto(
                        variation.Id,
                        variation.Name,
                        variation.SalePrice,
                        variation.SortOrder))
                    .ToArray());

        var menuItems = await appContext.DigitalMenuItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var categoryConfigs = menuItems
            .Where(m => string.Equals(m.ItemType, "CATEGORY", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(m => m.TargetId);

        var productConfigs = menuItems
            .Where(m => string.Equals(m.ItemType, "PRODUCT", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(m => m.TargetId);

        // Build sorted, active public categories and products
        var publicCategories = new List<PublicCategoryDto>();

        foreach (var category in rawCategories)
        {
            var isCategoryActive = true;
            var categoryOrder = int.MaxValue;

            if (categoryConfigs.TryGetValue(category.Id, out var catConfig))
            {
                isCategoryActive = catConfig.IsActive;
                categoryOrder = catConfig.SortOrder;
            }

            if (!isCategoryActive) continue;

            var categoryProducts = rawProducts
                .Where(p => p.CategoryId == category.Id)
                .Select(p =>
                {
                    var isProductActive = true;
                    var prodOrder = int.MaxValue;

                    if (productConfigs.TryGetValue(p.Id, out var prodConfig))
                    {
                        isProductActive = prodConfig.IsActive;
                        prodOrder = prodConfig.SortOrder;
                    }

                    return new
                    {
                        Product = p,
                        IsActive = isProductActive,
                        SortOrder = prodOrder
                    };
                })
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Product.Name)
                .Select(x => new PublicProductDto(
                    x.Product.Id,
                    x.Product.Name,
                    x.Product.Description,
                    x.Product.SalePrice,
                    x.Product.ImagePath ?? x.Product.ImageRef?.ToString("D"),
                    Image: null,
                    x.SortOrder,
                    variationsByProductId.GetValueOrDefault(x.Product.Id, []),
                    x.Product.ComboGroups
                        .Where(group => group.TenantId == tenantId)
                        .OrderBy(group => group.Order)
                        .ThenBy(group => group.CreatedAt)
                        .Select(group => new ProductComboGroupDto(
                            group.Id,
                            group.Name,
                            group.SelectionType.ToString().ToUpperInvariant(),
                            group.IsRequired,
                            group.MinSelections,
                            group.MaxSelections,
                            group.Order,
                            group.Options
                                .Where(option => option.TenantId == tenantId
                                    && option.Product.TenantId == tenantId
                                    && option.Product.IsActive
                                    && (option.ProductVariationId == null
                                        || option.ProductVariation is { IsActive: true } variation
                                            && variation.TenantId == tenantId))
                                .OrderBy(option => option.Order)
                                .ThenBy(option => option.CreatedAt)
                                .Select(option => new ProductComboOptionDto(
                                    option.Id,
                                    option.ProductId,
                                    option.Product.Name,
                                    option.ProductQuantity,
                                    option.PriceAdjustment,
                                    option.Order,
                                    option.ProductVariationId,
                                    option.ProductVariation?.Name))
                                .ToArray()))
                        .Where(group => group.Options.Count > 0)
                        .ToArray()
                ))
                .ToList();

            // Include category even if currently empty or has products
            publicCategories.Add(new PublicCategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.ImagePath ?? category.ImageRef?.ToString("D"),
                Image: null,
                categoryOrder,
                categoryProducts
            ));
        }

        var sortedCategories = publicCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();

        return new PublicDigitalMenuDto(
            TenantId: tenant.Id,
            OrganizationName: tenant.Name,
            HasLogo: tenant.HasLogo,
            Logo: fileStorage?.GetPublicUrl(tenant.LogoPath)
                ?? (tenant.LogoData is { Length: > 0 }
                    ? ToOptimizedDataUrl(tenant.LogoData, tenant.LogoContentType)
                    : null),
            LogoVersion: tenant.UpdatedAt.ToUnixTimeMilliseconds(),
            Phone: tenant.Phone,
            Address: tenant.Address,
            Website: tenant.Website,
            Style: style,
            Categories: sortedCategories
        );
    }

    public async Task<PublicDigitalMenuCategoryImagesDto?> GetPublicMenuCategoryImagesAsync(
        string slug,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var tenantId = await GetPublishedTenantIdAsync(slug, cancellationToken);
        if (!tenantId.HasValue) return null;

        var category = await appContext.Categories
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId.Value
                && item.Id == categoryId
                && item.IsActive)
            .Select(item => new { item.Id, item.ImagePath, item.ImageRef })
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null) return null;

        var menuItems = await appContext.DigitalMenuItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId.Value)
            .ToListAsync(cancellationToken);

        var categoryConfig = menuItems.FirstOrDefault(item =>
            string.Equals(item.ItemType, "CATEGORY", StringComparison.OrdinalIgnoreCase)
            && item.TargetId == categoryId);
        if (categoryConfig is { IsActive: false }) return null;

        var productConfigs = menuItems
            .Where(item => string.Equals(item.ItemType, "PRODUCT", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(item => item.TargetId);

        var products = (await appContext.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(product => product.TenantId == tenantId.Value
                && product.CategoryId == categoryId
                && product.IsActive)
            .Select(product => new { product.Id, product.ImagePath, product.ImageRef })
            .ToListAsync(cancellationToken))
            .Where(product => !productConfigs.TryGetValue(product.Id, out var config) || config.IsActive)
            .ToArray();

        var referencedImageIds = products
            .Select(product => product.ImageRef)
            .Append(category.ImageRef)
            .Where(imageId => imageId.HasValue)
            .Select(imageId => imageId!.Value)
            .ToHashSet();
        var entityIds = products
            .Select(product => product.Id.ToString())
            .Append(categoryId.ToString())
            .ToArray();

        var storedImages = await appContext.StoredImages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(image => image.TenantId == tenantId.Value
                && (image.Module == "products" || image.Module == "categories")
                && (referencedImageIds.Contains(image.Id)
                    || (image.EntityId != null && entityIds.Contains(image.EntityId))))
            .OrderByDescending(image => image.UpdatedAt)
            .ToListAsync(cancellationToken);

        var imagesById = storedImages.ToDictionary(image => image.Id);
        var imagesByEntity = storedImages
            .Where(image => !string.IsNullOrWhiteSpace(image.EntityId))
            .GroupBy(image => (Module: image.Module.ToUpperInvariant(), EntityId: image.EntityId!))
            .ToDictionary(group => group.Key, group => group.First());

        var categoryImage = ResolveStoredImage(
            category.ImageRef,
            "categories",
            categoryId,
            imagesById,
            imagesByEntity);
        var productImages = products
            .Select(product => new PublicDigitalMenuProductImageDto(
                product.Id,
                fileStorage?.GetPublicUrl(product.ImagePath)
                    ?? ToOptimizedDataUrl(ResolveStoredImage(
                        product.ImageRef,
                        "products",
                        product.Id,
                        imagesById,
                        imagesByEntity))))
            .ToArray();

        return new PublicDigitalMenuCategoryImagesDto(
            categoryId,
            fileStorage?.GetPublicUrl(category.ImagePath) ?? ToOptimizedDataUrl(categoryImage),
            productImages);
    }

    private static DigitalMenuStyleDto ParseStyle(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new DigitalMenuStyleDto();
        try
        {
            return JsonSerializer.Deserialize<DigitalMenuStyleDto>(json, JsonOptions) ?? new DigitalMenuStyleDto();
        }
        catch
        {
            return new DigitalMenuStyleDto();
        }
    }

    private static StoredImage? ResolveStoredImage(
        Guid? imageRef,
        string module,
        Guid entityId,
        IReadOnlyDictionary<Guid, StoredImage> imagesById,
        IReadOnlyDictionary<(string Module, string EntityId), StoredImage> imagesByEntity)
    {
        if (imageRef.HasValue && imagesById.TryGetValue(imageRef.Value, out var referencedImage))
        {
            return referencedImage;
        }

        return imagesByEntity.GetValueOrDefault((module.ToUpperInvariant(), entityId.ToString()));
    }

    private async Task<Guid?> GetPublishedTenantIdAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var tenantId = await appContext.OrganizationParameters
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(parameter => parameter.Key == OrganizationParameterKeys.DigitalMenuSlug
                && parameter.Value.ToLower() == normalizedSlug)
            .Select(parameter => (Guid?)parameter.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!tenantId.HasValue) return null;

        var isEnabled = await appContext.OrganizationParameters
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(parameter => parameter.TenantId == tenantId.Value
                && parameter.Key == OrganizationParameterKeys.EnableDigitalMenu
                && parameter.Value.ToLower() == "true", cancellationToken);

        if (!isEnabled) return null;

        return await platformContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId.Value && tenant.IsActive)
            .Select(tenant => (Guid?)tenant.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static byte[]? DecodeStoredImage(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var separatorIndex = content.IndexOf(',');
        var rawBase64 = content.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && separatorIndex >= 0
            ? content[(separatorIndex + 1)..]
            : content;

        try
        {
            return Convert.FromBase64String(rawBase64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? ToOptimizedDataUrl(StoredImage? image)
    {
        if (image is null) return null;
        var content = DecodeStoredImage(image.Base64Content);
        return content is null
            ? null
            : ToOptimizedDataUrl(content, image.ContentType);
    }

    private static string ToOptimizedDataUrl(
        byte[] source,
        string? sourceContentType)
    {
        var optimized = OptimizePublicImage(source, sourceContentType);
        return $"data:{optimized.ContentType};base64,{Convert.ToBase64String(optimized.Content)}";
    }

    private static OptimizedPublicImage OptimizePublicImage(
        byte[] source,
        string? sourceContentType)
    {
        try
        {
            using var image = Image.Load(source);
            image.Mutate(operation => operation.AutoOrient());
            ResizeToFit(image, PublicImageMaxSize);

            var encoded = EncodeWebp(image, quality: 58);

            if (encoded.Length > PublicImageTargetBytes)
            {
                ResizeToFit(image, PublicImageCompactSize);
                encoded = EncodeWebp(image, quality: 45);
            }

            if (encoded.Length > PublicImageTargetBytes)
            {
                ResizeToFit(image, PublicImageThumbnailSize);
                encoded = EncodeWebp(image, quality: 38);
            }

            return new OptimizedPublicImage(encoded, "image/webp");
        }
        catch (UnknownImageFormatException)
        {
            return OriginalPublicImage(source, sourceContentType);
        }
        catch (InvalidImageContentException)
        {
            return OriginalPublicImage(source, sourceContentType);
        }
    }

    private static OptimizedPublicImage OriginalPublicImage(
        byte[] source,
        string? sourceContentType)
        => new(
            source,
            string.IsNullOrWhiteSpace(sourceContentType) ? "application/octet-stream" : sourceContentType);

    private static void ResizeToFit(Image image, int maximumSize)
    {
        if (image.Width <= maximumSize && image.Height <= maximumSize) return;

        image.Mutate(operation => operation.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maximumSize, maximumSize)
        }));
    }

    private static byte[] EncodeWebp(Image image, int quality)
    {
        using var output = new MemoryStream();
        image.Save(output, new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossy,
            Quality = quality,
            Method = WebpEncodingMethod.Level4,
            SkipMetadata = true,
            UseAlphaCompression = true
        });
        return output.ToArray();
    }

    private sealed record OptimizedPublicImage(byte[] Content, string ContentType);
}
