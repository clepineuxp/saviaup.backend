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
    ApplicationDbContext appContext) : IDigitalMenuRepository
{
    private const int PublicImageMaxSize = 960;
    private const int PublicImageQuality = 74;
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
                HasLogo = item.LogoData != null && item.LogoData.Length > 0,
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

        var storedImages = await appContext.StoredImages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(image => image.TenantId == tenantId
                && (image.Module == "products" || image.Module == "categories"))
            .OrderByDescending(image => image.UpdatedAt)
            .Select(image => new PublicImageReference(image.Id, image.Module, image.EntityId))
            .ToListAsync(cancellationToken);
        var imagesById = storedImages.ToDictionary(image => image.Id);
        var imagesByEntity = storedImages
            .Where(image => !string.IsNullOrWhiteSpace(image.EntityId))
            .GroupBy(image => (Module: image.Module.ToUpperInvariant(), EntityId: image.EntityId!))
            .ToDictionary(group => group.Key, group => group.First());

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
                    x.Product.ImageRef,
                    ResolveImageUrl(normalizedSlug, x.Product.ImageRef, "products", x.Product.Id, imagesById, imagesByEntity),
                    x.SortOrder,
                    variationsByProductId.GetValueOrDefault(x.Product.Id, [])
                ))
                .ToList();

            // Include category even if currently empty or has products
            publicCategories.Add(new PublicCategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.ImageRef,
                ResolveImageUrl(normalizedSlug, category.ImageRef, "categories", category.Id, imagesById, imagesByEntity),
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
            Logo: tenant.HasLogo
                ? $"/api/public/menu/{Uri.EscapeDataString(normalizedSlug)}/logo?v={tenant.UpdatedAt.ToUnixTimeMilliseconds()}"
                : null,
            LogoVersion: tenant.UpdatedAt.ToUnixTimeMilliseconds(),
            Phone: tenant.Phone,
            Address: tenant.Address,
            Website: tenant.Website,
            Style: style,
            Categories: sortedCategories
        );
    }

    public async Task<PublicDigitalMenuImageDto?> GetPublicMenuImageAsync(
        string slug,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var tenantId = await GetPublishedTenantIdAsync(slug, cancellationToken);
        if (!tenantId.HasValue) return null;

        var storedImage = await appContext.StoredImages
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                image => image.TenantId == tenantId.Value
                    && image.Id == imageId
                    && (image.Module == "products" || image.Module == "categories"),
                cancellationToken);

        if (storedImage is null || !await IsImagePublishedAsync(tenantId.Value, storedImage, cancellationToken))
        {
            return null;
        }

        var content = DecodeStoredImage(storedImage.Base64Content);
        return content is null
            ? null
            : OptimizePublicImage(content, storedImage.ContentType, storedImage.UpdatedAt);
    }

    public async Task<PublicDigitalMenuImageDto?> GetPublicMenuLogoAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var tenantId = await GetPublishedTenantIdAsync(slug, cancellationToken);
        if (!tenantId.HasValue) return null;

        var tenant = await platformContext.Tenants
            .AsNoTracking()
            .Where(item => item.Id == tenantId.Value && item.IsActive && item.LogoData != null)
            .Select(item => new { item.LogoData, item.LogoContentType, item.UpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return tenant?.LogoData is { Length: > 0 }
            ? OptimizePublicImage(tenant.LogoData, tenant.LogoContentType, tenant.UpdatedAt)
            : null;
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

    private static string? ResolveImageUrl(
        string slug,
        Guid? imageRef,
        string module,
        Guid entityId,
        IReadOnlyDictionary<Guid, PublicImageReference> imagesById,
        IReadOnlyDictionary<(string Module, string EntityId), PublicImageReference> imagesByEntity)
    {
        var resolvedId = imageRef.HasValue && imagesById.ContainsKey(imageRef.Value)
            ? imageRef
            : imagesByEntity.GetValueOrDefault((module.ToUpperInvariant(), entityId.ToString()))?.Id;

        return resolvedId.HasValue
            ? $"/api/public/menu/{Uri.EscapeDataString(slug)}/images/{resolvedId.Value:D}"
            : null;
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

    private async Task<bool> IsImagePublishedAsync(
        Guid tenantId,
        StoredImage image,
        CancellationToken cancellationToken)
    {
        _ = Guid.TryParse(image.EntityId, out var entityId);

        if (string.Equals(image.Module, "products", StringComparison.OrdinalIgnoreCase))
        {
            var product = await appContext.Products
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(product => product.TenantId == tenantId
                    && product.IsActive
                    && (product.ImageRef == image.Id || (product.ImageRef == null && product.Id == entityId)))
                .Select(product => new { product.Id, product.CategoryId })
                .FirstOrDefaultAsync(cancellationToken);

            if (product is null) return false;

            var categoryIsPublished = await appContext.Categories
                .AsNoTracking()
                .IgnoreQueryFilters()
                .AnyAsync(category => category.TenantId == tenantId
                    && category.Id == product.CategoryId
                    && category.IsActive, cancellationToken);

            if (!categoryIsPublished) return false;

            return !await appContext.DigitalMenuItems
                .AsNoTracking()
                .IgnoreQueryFilters()
                .AnyAsync(item => item.TenantId == tenantId
                    && !item.IsActive
                    && ((item.ItemType == "PRODUCT" && item.TargetId == product.Id)
                        || (item.ItemType == "CATEGORY" && item.TargetId == product.CategoryId)), cancellationToken);
        }

        if (!string.Equals(image.Module, "categories", StringComparison.OrdinalIgnoreCase)) return false;

        var categoryId = await appContext.Categories
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(category => category.TenantId == tenantId
                && category.IsActive
                && (category.ImageRef == image.Id || (category.ImageRef == null && category.Id == entityId)))
            .Select(category => (Guid?)category.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return categoryId.HasValue && !await appContext.DigitalMenuItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(item => item.TenantId == tenantId
                && item.ItemType == "CATEGORY"
                && item.TargetId == categoryId.Value
                && !item.IsActive, cancellationToken);
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

    private static PublicDigitalMenuImageDto OptimizePublicImage(
        byte[] source,
        string? sourceContentType,
        DateTimeOffset updatedAt)
    {
        try
        {
            using var image = Image.Load(source);
            image.Mutate(operation => operation.AutoOrient());
            if (image.Width > PublicImageMaxSize || image.Height > PublicImageMaxSize)
            {
                image.Mutate(operation => operation.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(PublicImageMaxSize, PublicImageMaxSize)
                }));
            }

            using var output = new MemoryStream();
            image.Save(output, new WebpEncoder { Quality = PublicImageQuality });
            return new PublicDigitalMenuImageDto(
                output.ToArray(),
                "image/webp",
                updatedAt.ToUnixTimeMilliseconds());
        }
        catch (UnknownImageFormatException)
        {
            return OriginalPublicImage(source, sourceContentType, updatedAt);
        }
        catch (InvalidImageContentException)
        {
            return OriginalPublicImage(source, sourceContentType, updatedAt);
        }
    }

    private static PublicDigitalMenuImageDto OriginalPublicImage(
        byte[] source,
        string? sourceContentType,
        DateTimeOffset updatedAt)
        => new(
            source,
            string.IsNullOrWhiteSpace(sourceContentType) ? "application/octet-stream" : sourceContentType,
            updatedAt.ToUnixTimeMilliseconds());

    private sealed record PublicImageReference(Guid Id, string Module, string? EntityId);
}
