using System.Text.Json;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.DigitalMenu;

public sealed class DigitalMenuUseCase(
    ISettingsRepository settingsRepository,
    IDigitalMenuRepository digitalMenuRepository,
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IDigitalMenuUseCase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<DigitalMenuConfigDto>> GetConfigAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await settingsRepository.GetTenantForUpdateAsync(tenantId, cancellationToken) is null)
        {
            return Result<DigitalMenuConfigDto>.Failure(Errors.TenantNotFound);
        }

        var parameters = (await settingsRepository.GetParametersAsync(tenantId, cancellationToken))
            .ToDictionary(item => item.Key, item => item.Value);

        var enabled = parameters.TryGetValue(SettingsDefaults.EnableDigitalMenu, out var e) && bool.TryParse(e, out var parsedE) && parsedE;
        var slug = parameters.TryGetValue(SettingsDefaults.DigitalMenuSlug, out var s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;
        var canEditSlug = string.IsNullOrWhiteSpace(slug);

        var styleJson = parameters.TryGetValue(SettingsDefaults.DigitalMenuStyle, out var st) ? st : null;
        var style = ParseStyle(styleJson);

        var existingItems = await digitalMenuRepository.GetItemsAsync(tenantId, cancellationToken);
        var categoryConfigs = existingItems
            .Where(m => string.Equals(m.ItemType, "CATEGORY", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(m => m.TargetId);

        var productConfigs = existingItems
            .Where(m => string.Equals(m.ItemType, "PRODUCT", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(m => m.TargetId);

        var categories = await categoryRepository.GetForTenantAsync(tenantId, includeInactive: false, cancellationToken);
        var products = await productRepository.GetAllForTenantAsync(tenantId, includeInactive: false, cancellationToken);

        var categoryDtos = categories
            .Select(c =>
            {
                var hasConfig = categoryConfigs.TryGetValue(c.Id, out var config);
                return new DigitalMenuItemSummaryDto(
                    Id: config?.Id,
                    ItemType: "CATEGORY",
                    TargetId: c.Id,
                    CategoryId: null,
                    Name: c.Name,
                    Description: c.Description,
                    Price: null,
                    ImageRef: c.ImageRef,
                    SortOrder: hasConfig ? config!.SortOrder : 999,
                    IsActive: !hasConfig || config!.IsActive
                );
            })
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToArray();

        var productDtos = products
            .Select(p =>
            {
                var hasConfig = productConfigs.TryGetValue(p.Id, out var config);
                return new DigitalMenuItemSummaryDto(
                    Id: config?.Id,
                    ItemType: "PRODUCT",
                    TargetId: p.Id,
                    CategoryId: p.CategoryId,
                    Name: p.Name,
                    Description: p.Description,
                    Price: p.SalePrice,
                    ImageRef: p.ImageRef,
                    SortOrder: hasConfig ? config!.SortOrder : 999,
                    IsActive: !hasConfig || config!.IsActive
                );
            })
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToArray();

        var configDto = new DigitalMenuConfigDto(
            Enabled: enabled,
            Slug: slug,
            CanEditSlug: canEditSlug,
            Style: style,
            Categories: categoryDtos,
            Products: productDtos
        );

        return Result<DigitalMenuConfigDto>.Success(configDto);
    }

    public async Task<Result> UpdateParametersAsync(Guid tenantId, UpdateDigitalMenuParametersRequest request, CancellationToken cancellationToken)
    {
        var tenant = await settingsRepository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(Errors.TenantNotFound);

        var parameters = (await settingsRepository.GetParametersAsync(tenantId, cancellationToken))
            .ToDictionary(item => item.Key);

        var currentSlug = parameters.TryGetValue(SettingsDefaults.DigitalMenuSlug, out var pSlug) ? pSlug.Value.Trim().ToLowerInvariant() : string.Empty;
        var incomingSlug = request.Slug?.Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(incomingSlug))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(incomingSlug, "^[a-z0-9-]+$"))
            {
                return Result.Failure(Errors.Validation);
            }

            if (!string.IsNullOrEmpty(currentSlug) && !string.Equals(currentSlug, incomingSlug, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(Errors.DigitalMenuSlugImmutable);
            }

            if (string.IsNullOrEmpty(currentSlug) && await settingsRepository.DigitalMenuSlugExistsAsync(incomingSlug, tenantId, cancellationToken))
            {
                return Result.Failure(Errors.DigitalMenuSlugAlreadyExists);
            }
        }

        var effectiveSlug = !string.IsNullOrEmpty(incomingSlug) ? incomingSlug : currentSlug;
        if (request.Enabled && string.IsNullOrWhiteSpace(effectiveSlug))
        {
            return Result.Failure(Errors.DigitalMenuSlugRequired);
        }

        var now = clock.UtcNow;
        var values = new Dictionary<string, (string Value, string Type)>
        {
            [SettingsDefaults.EnableDigitalMenu] = (request.Enabled.ToString().ToLowerInvariant(), "boolean")
        };

        if (!string.IsNullOrEmpty(effectiveSlug))
        {
            values[SettingsDefaults.DigitalMenuSlug] = (effectiveSlug, "string");
        }

        var additions = new List<OrganizationParameter>();
        foreach (var (key, val) in values)
        {
            if (parameters.TryGetValue(key, out var parameter))
            {
                parameter.Value = val.Value;
                parameter.ValueType = val.Type;
                parameter.UpdatedAt = now;
            }
            else
            {
                additions.Add(new OrganizationParameter
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Key = key,
                    Value = val.Value,
                    ValueType = val.Type,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (additions.Count > 0)
        {
            await settingsRepository.AddParametersAsync(additions, cancellationToken);
        }

        tenant.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateItemsAsync(Guid tenantId, SaveDigitalMenuItemsRequest request, Guid? userId, string? userName, CancellationToken cancellationToken)
    {
        var tenant = await settingsRepository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(Errors.TenantNotFound);

        if (request.Items is null || request.Items.Count == 0)
        {
            return Result.Failure(Errors.Validation);
        }

        var categories = await categoryRepository.GetForTenantAsync(tenantId, includeInactive: false, cancellationToken);
        var products = await productRepository.GetAllForTenantAsync(tenantId, includeInactive: false, cancellationToken);
        var categoryIds = categories.Select(category => category.Id).ToHashSet();
        var productCategories = products.ToDictionary(product => product.Id, product => product.CategoryId);
        var configuredItems = new HashSet<(string ItemType, Guid TargetId)>();

        foreach (var item in request.Items)
        {
            var itemType = item.ItemType?.Trim().ToUpperInvariant();
            if (item.TargetId == Guid.Empty || item.SortOrder < 1 || itemType is not ("CATEGORY" or "PRODUCT") || !configuredItems.Add((itemType, item.TargetId)))
            {
                return Result.Failure(Errors.Validation);
            }

            if (itemType == "CATEGORY")
            {
                if (item.CategoryId.HasValue || !categoryIds.Contains(item.TargetId))
                {
                    return Result.Failure(Errors.Validation);
                }
            }
            else if (!item.CategoryId.HasValue
                     || !productCategories.TryGetValue(item.TargetId, out var productCategoryId)
                     || productCategoryId != item.CategoryId.Value)
            {
                return Result.Failure(Errors.Validation);
            }
        }

        var now = clock.UtcNow;
        var itemsToSave = request.Items.Select(item => new DigitalMenuItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ItemType = item.ItemType.ToUpperInvariant(),
            TargetId = item.TargetId,
            CategoryId = item.CategoryId,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            LastModifiedByUserId = userId,
            LastModifiedByUserName = userName,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        await digitalMenuRepository.ReplaceItemsAsync(tenantId, itemsToSave, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateStyleAsync(Guid tenantId, DigitalMenuStyleDto request, CancellationToken cancellationToken)
    {
        var tenant = await settingsRepository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(Errors.TenantNotFound);

        var parameters = (await settingsRepository.GetParametersAsync(tenantId, cancellationToken))
            .ToDictionary(item => item.Key);

        var now = clock.UtcNow;
        var json = JsonSerializer.Serialize(request, JsonOptions);

        if (parameters.TryGetValue(SettingsDefaults.DigitalMenuStyle, out var styleParam))
        {
            styleParam.Value = json;
            styleParam.ValueType = "json";
            styleParam.UpdatedAt = now;
        }
        else
        {
            await settingsRepository.AddParametersAsync(
            [
                new OrganizationParameter
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Key = SettingsDefaults.DigitalMenuStyle,
                    Value = json,
                    ValueType = "json",
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ], cancellationToken);
        }

        tenant.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PublicDigitalMenuDto>> GetPublicMenuAsync(string slug, CancellationToken cancellationToken)
    {
        var menu = await digitalMenuRepository.GetPublicMenuAsync(slug, cancellationToken);
        if (menu is null)
        {
            return Result<PublicDigitalMenuDto>.Failure(Errors.DigitalMenuNotFound);
        }

        return Result<PublicDigitalMenuDto>.Success(menu);
    }

    public async Task<Result<PublicDigitalMenuImageDto>> GetPublicMenuImageAsync(
        string slug,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var image = await digitalMenuRepository.GetPublicMenuImageAsync(slug, imageId, cancellationToken);
        return image is null
            ? Result<PublicDigitalMenuImageDto>.Failure(Errors.DigitalMenuNotFound)
            : Result<PublicDigitalMenuImageDto>.Success(image);
    }

    public async Task<Result<PublicDigitalMenuImageDto>> GetPublicMenuLogoAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var image = await digitalMenuRepository.GetPublicMenuLogoAsync(slug, cancellationToken);
        return image is null
            ? Result<PublicDigitalMenuImageDto>.Failure(Errors.DigitalMenuNotFound)
            : Result<PublicDigitalMenuImageDto>.Success(image);
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
}
