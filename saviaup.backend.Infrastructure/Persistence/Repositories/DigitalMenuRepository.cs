using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);

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
                    ResolveImage(x.Product.ImageRef, "products", x.Product.Id, imagesById, imagesByEntity),
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
                ResolveImage(category.ImageRef, "categories", category.Id, imagesById, imagesByEntity),
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
            HasLogo: tenant.LogoData is { Length: > 0 },
            Logo: ToDataUrl(tenant.LogoData, tenant.LogoContentType),
            LogoVersion: tenant.UpdatedAt.ToUnixTimeMilliseconds(),
            Phone: tenant.Phone,
            Address: tenant.Address,
            Website: tenant.Website,
            Style: style,
            Categories: sortedCategories
        );
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

    private static string? ResolveImage(
        Guid? imageRef,
        string module,
        Guid entityId,
        IReadOnlyDictionary<Guid, StoredImage> imagesById,
        IReadOnlyDictionary<(string Module, string EntityId), StoredImage> imagesByEntity)
    {
        if (imageRef.HasValue && imagesById.TryGetValue(imageRef.Value, out var referencedImage))
        {
            return ToDataUrl(referencedImage.Base64Content, referencedImage.ContentType);
        }

        return imagesByEntity.TryGetValue((module.ToUpperInvariant(), entityId.ToString()), out var entityImage)
            ? ToDataUrl(entityImage.Base64Content, entityImage.ContentType)
            : null;
    }

    private static string? ToDataUrl(byte[]? content, string? contentType)
        => content is { Length: > 0 }
            ? $"data:{contentType ?? "image/png"};base64,{Convert.ToBase64String(content)}"
            : null;

    private static string? ToDataUrl(string? content, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        return content.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? content
            : $"data:{contentType ?? "image/png"};base64,{content}";
    }
}
