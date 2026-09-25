using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Images;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Products;

public sealed class ListProductsUseCase(IProductRepository repository) : IListProductsUseCase
{
    public async Task<Result<PagedResponse<ProductDto>>> ExecuteAsync(
        Guid tenantId, ProductQueryRequest request, CancellationToken cancellationToken)
    {
        ProductType? type = null;
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (!ProductRules.TryParseType(request.Type, out var parsedType))
                return Result<PagedResponse<ProductDto>>.Failure(Errors.Validation);
            type = parsedType;
        }

        var page = await repository.GetPageAsync(tenantId, request, type, cancellationToken);
        return Result<PagedResponse<ProductDto>>.Success(ProductRules.ToPage(page, request.Page, request.PageSize));
    }
}

public sealed class CreateProductUseCase(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    IFileStorage? fileStorage = null,
    ITableRealtimeNotifier? realtime = null) : ICreateProductUseCase
{
    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid tenantId, Guid userId, string userName, CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty
            || !ProductRules.TryPrepare(
                request.Type,
                request.Name,
                request.Description,
                request.Image,
                request.SalePrice,
                request.PreparationTimeMinutes,
                out var values)
            || !ProductRules.TryValidateVariations(
                values.Type,
                values.SalePrice,
                request.Variations,
                out var effectiveSalePrice)
            || !ProductRules.TryValidateComboGroups(values.Type, request.ComboGroups)
            || (values.Type == ProductType.Combo && request.Recipe is { Count: > 0 }))
            return Result<ProductDto>.Failure(Errors.Validation);

        if (!ImageHelper.IsReferenceOwnedByTenant(values.Image, tenantId))
            return Result<ProductDto>.Failure(Errors.Validation);

        values = values with { SalePrice = effectiveSalePrice };

        var category = await categoryRepository.GetByIdAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result<ProductDto>.Failure(Errors.CategoryNotFound);

        var productId = Guid.NewGuid();
        var comboProducts = await LoadComboProductsAsync(
            tenantId, productId, values.Type, request.ComboGroups, productRepository, cancellationToken);
        if (comboProducts is null) return Result<ProductDto>.Failure(Errors.Validation);

        var now = clock.UtcNow;
        string? imagePath = null;

        if (!string.IsNullOrWhiteSpace(values.Image)
            && values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            if (fileStorage is null) return Result<ProductDto>.Failure(Errors.Validation);
            try
            {
                var storedImage = await ImageHelper.SaveDataUrlAsync(
                    fileStorage, tenantId, "products", productId.ToString("D"), values.Image, cancellationToken);
                if (storedImage is null) return Result<ProductDto>.Failure(Errors.Validation);
                imagePath = storedImage.Reference;
            }
            catch (InvalidDataException)
            {
                return Result<ProductDto>.Failure(Errors.Validation);
            }
        }
        else if (!string.IsNullOrWhiteSpace(values.Image)
            && !values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            imagePath = values.Image;

        var product = new Product
        {
            Id = productId,
            TenantId = tenantId,
            CategoryId = category.Id,
            Category = category,
            Type = values.Type,
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            Description = values.Description,
            ImagePath = imagePath,
            SalePrice = values.SalePrice,
            PreparationTimeMinutes = values.PreparationTimeMinutes,
            IsInventoryTracked = values.Type == ProductType.Normal && category.IsInventoryTracked && request.IsInventoryTracked,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            LastModifiedByUserId = userId,
            LastModifiedByUserName = userName,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.Recipe is not null && request.Recipe.Count > 0)
        {
            var orderIdx = 0;
            foreach (var r in request.Recipe)
            {
                if (r.Quantity <= 0) continue;
                var hasIng = r.IngredientId.HasValue && r.IngredientId.Value != Guid.Empty;
                var hasCustom = !string.IsNullOrWhiteSpace(r.CustomIngredientName);
                if (!hasIng && !hasCustom) continue;

                product.RecipeItems.Add(new ProductRecipeItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = productId,
                    IngredientId = hasIng ? r.IngredientId : null,
                    CustomIngredientName = hasCustom ? r.CustomIngredientName!.Trim() : null,
                    Quantity = r.Quantity,
                    Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim(),
                    Order = r.Order > 0 ? r.Order : orderIdx++,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (request.Variations is not null && request.Variations.Count > 0)
        {
            var varIdx = 0;
            foreach (var v in request.Variations)
            {
                var vName = v.Name?.Trim() ?? string.Empty;
                product.Variations.Add(new ProductVariation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = productId,
                    Name = vName,
                    NormalizedName = vName.ToUpperInvariant(),
                    SalePrice = v.SalePrice,
                    Order = v.Order > 0 ? v.Order : varIdx++,
                    IsActive = v.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (values.Type == ProductType.Combo && request.ComboGroups is { Count: > 0 })
        {
            foreach (var group in ProductRules.CreateComboGroups(
                tenantId, productId, request.ComboGroups, now))
                product.ComboGroups.Add(group);
        }

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (realtime is not null)
            await realtime.SalesDataInvalidatedAsync(tenantId, new(["products"], now), cancellationToken);
        var reloaded = await productRepository.GetByIdAsync(tenantId, productId, cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(reloaded ?? product));
    }

    private static async Task<IReadOnlyDictionary<Guid, Product>?> LoadComboProductsAsync(
        Guid tenantId,
        Guid comboProductId,
        ProductType type,
        IReadOnlyCollection<ProductComboGroupRequest>? groups,
        IProductRepository repository,
        CancellationToken cancellationToken)
    {
        if (type == ProductType.Normal) return new Dictionary<Guid, Product>();
        var ids = groups!.SelectMany(group => group.Options).Select(option => option.ProductId).Distinct().ToArray();
        var products = await repository.GetByIdsWithRecipesAsync(tenantId, ids, cancellationToken);
        if (products.Count != ids.Length
            || products.Any(product => product.Id == comboProductId || !product.IsActive || product.Type != ProductType.Normal))
            return null;
        var productsById = products.ToDictionary(product => product.Id);
        if (groups!.SelectMany(group => group.Options).Any(option =>
                !productsById.TryGetValue(option.ProductId, out var product)
                || (option.ProductVariationId.HasValue
                    ? product.Variations.All(variation =>
                        variation.Id != option.ProductVariationId.Value || !variation.IsActive)
                    : product.Variations.Count > 0)))
            return null;
        return productsById;
    }
}

public sealed class UpdateProductUseCase(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    IFileStorage? fileStorage = null,
    ITableRealtimeNotifier? realtime = null) : IUpdateProductUseCase
{
    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid tenantId, Guid productId, Guid userId, string userName, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty
            || !ProductRules.TryPrepare(
                request.Type,
                request.Name,
                request.Description,
                request.Image,
                request.SalePrice,
                request.PreparationTimeMinutes,
                out var values)
            || !ProductRules.TryValidateVariations(
                values.Type,
                values.SalePrice,
                request.Variations,
                out var effectiveSalePrice)
            || !ProductRules.TryValidateComboGroups(values.Type, request.ComboGroups)
            || (values.Type == ProductType.Combo && request.Recipe is { Count: > 0 }))
            return Result<ProductDto>.Failure(Errors.Validation);

        if (!ImageHelper.IsReferenceOwnedByTenant(values.Image, tenantId))
            return Result<ProductDto>.Failure(Errors.Validation);

        values = values with { SalePrice = effectiveSalePrice };

        // GetByIdForUpdateAsync carga el producto SIN RecipeItems en el ChangeTracker.
        // Esto evita el DbUpdateConcurrencyException que ocurría al intentar gestionar
        // los RecipeItems antiguos (ya rastreados) junto con los nuevos en el mismo contexto.
        var product = await productRepository.GetByIdForUpdateAsync(tenantId, productId, cancellationToken);
        if (product is null) return Result<ProductDto>.Failure(Errors.ProductNotFound);
        if (product.Type == ProductType.Normal
            && values.Type == ProductType.Combo
            && await productRepository.IsUsedInComboAsync(tenantId, productId, cancellationToken))
            return Result<ProductDto>.Failure(Errors.ProductInUse);

        // AsNoTracking porque GetByIdForUpdateAsync ya tiene product.Category en el tracker.
        var category = await categoryRepository.GetByIdAsNoTrackingAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result<ProductDto>.Failure(Errors.CategoryNotFound);

        var comboProducts = await LoadComboProductsAsync(
            tenantId, productId, values.Type, request.ComboGroups, productRepository, cancellationToken);
        if (comboProducts is null) return Result<ProductDto>.Failure(Errors.Validation);

        var now = clock.UtcNow;
        var previousImagePath = product.ImagePath;

        if (!string.IsNullOrWhiteSpace(values.Image)
            && values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            if (fileStorage is null) return Result<ProductDto>.Failure(Errors.Validation);
            try
            {
                var storedImage = await ImageHelper.SaveDataUrlAsync(
                    fileStorage, tenantId, "products", productId.ToString("D"), values.Image, cancellationToken);
                if (storedImage is null) return Result<ProductDto>.Failure(Errors.Validation);
                product.ImagePath = storedImage.Reference;
                product.ImageRef = null;
                product.ImageStored = null;
            }
            catch (InvalidDataException)
            {
                return Result<ProductDto>.Failure(Errors.Validation);
            }
        }
        else if (string.IsNullOrWhiteSpace(values.Image))
        {
            product.ImagePath = null;
            product.ImageRef = null;
            product.ImageStored = null;
        }
        else if (!values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            product.ImagePath = values.Image;
            product.ImageRef = null;
            product.ImageStored = null;
        }

        product.CategoryId = category.Id;
        product.Type = values.Type;
        product.Name = values.Name;
        product.NormalizedName = values.NormalizedName;
        product.Description = values.Description;
        product.SalePrice = values.SalePrice;
        product.PreparationTimeMinutes = values.PreparationTimeMinutes;
        product.IsInventoryTracked = values.Type == ProductType.Normal && category.IsInventoryTracked && request.IsInventoryTracked;
        product.LastModifiedByUserId = userId;
        product.LastModifiedByUserName = userName;
        product.UpdatedAt = now;

        if (request.Recipe is not null || values.Type == ProductType.Combo)
        {
            await productRepository.DeleteRecipeItemsAsync(tenantId, productId, cancellationToken);

            var itemsToAdd = new List<ProductRecipeItem>();
            var orderIdx = 0;
            foreach (var r in request.Recipe ?? [])
            {
                if (r.Quantity <= 0) continue;
                var hasIng = r.IngredientId.HasValue && r.IngredientId.Value != Guid.Empty;
                var hasCustom = !string.IsNullOrWhiteSpace(r.CustomIngredientName);
                if (!hasIng && !hasCustom) continue;

                itemsToAdd.Add(new ProductRecipeItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = product.Id,
                    IngredientId = hasIng ? r.IngredientId : null,
                    CustomIngredientName = hasCustom ? r.CustomIngredientName!.Trim() : null,
                    Quantity = r.Quantity,
                    Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim(),
                    Order = r.Order > 0 ? r.Order : orderIdx++,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (itemsToAdd.Count > 0)
            {
                await productRepository.AddRecipeItemsAsync(itemsToAdd, cancellationToken);
            }
        }

        if (request.Variations is not null || values.Type == ProductType.Combo)
        {
            var existingVariations = await productRepository.GetVariationsForUpdateAsync(
                tenantId, productId, cancellationToken);
            var existingById = existingVariations.ToDictionary(variation => variation.Id);
            var usedVariationIds = await productRepository.GetVariationIdsUsedInComboAsync(
                tenantId, productId, cancellationToken);
            var varsToAdd = new List<ProductVariation>();
            var retainedIds = new HashSet<Guid>();
            var varIdx = 0;
            foreach (var v in request.Variations ?? [])
            {
                var vName = v.Name?.Trim() ?? string.Empty;
                if (v.Id.HasValue && existingById.TryGetValue(v.Id.Value, out var existingVariation))
                {
                    if (!v.IsActive && usedVariationIds.Contains(existingVariation.Id))
                        return Result<ProductDto>.Failure(Errors.ProductInUse);
                    existingVariation.Name = vName;
                    existingVariation.NormalizedName = vName.ToUpperInvariant();
                    existingVariation.SalePrice = v.SalePrice;
                    existingVariation.Order = v.Order > 0 ? v.Order : varIdx++;
                    existingVariation.IsActive = v.IsActive;
                    existingVariation.UpdatedAt = now;
                    retainedIds.Add(existingVariation.Id);
                    continue;
                }

                varsToAdd.Add(new ProductVariation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = product.Id,
                    Name = vName,
                    NormalizedName = vName.ToUpperInvariant(),
                    SalePrice = v.SalePrice,
                    Order = v.Order > 0 ? v.Order : varIdx++,
                    IsActive = v.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            var variationsToRemove = existingVariations
                .Where(variation => !retainedIds.Contains(variation.Id))
                .ToArray();
            if (variationsToRemove.Any(variation => usedVariationIds.Contains(variation.Id)))
                return Result<ProductDto>.Failure(Errors.ProductInUse);
            productRepository.RemoveVariations(variationsToRemove);

            if (varsToAdd.Count > 0)
            {
                await productRepository.AddVariationsAsync(varsToAdd, cancellationToken);
            }
        }

        if (request.ComboGroups is not null || values.Type == ProductType.Normal)
        {
            await productRepository.DeleteComboGroupsAsync(tenantId, productId, cancellationToken);
            if (values.Type == ProductType.Combo && request.ComboGroups is { Count: > 0 })
            {
                var groups = ProductRules.CreateComboGroups(
                    tenantId, productId, request.ComboGroups, now);
                await productRepository.AddComboGroupsAsync(groups, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (fileStorage is not null
            && previousImagePath != product.ImagePath
            && ImageHelper.IsManagedReference(previousImagePath))
            await fileStorage.DeleteAsync(tenantId, previousImagePath, cancellationToken);
        if (realtime is not null)
            await realtime.SalesDataInvalidatedAsync(tenantId, new(["products"], now), cancellationToken);
        var reloaded = await productRepository.GetByIdAsync(tenantId, product.Id, cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(reloaded ?? product));
    }

    private static async Task<IReadOnlyDictionary<Guid, Product>?> LoadComboProductsAsync(
        Guid tenantId,
        Guid comboProductId,
        ProductType type,
        IReadOnlyCollection<ProductComboGroupRequest>? groups,
        IProductRepository repository,
        CancellationToken cancellationToken)
    {
        if (type == ProductType.Normal) return new Dictionary<Guid, Product>();
        var ids = groups!.SelectMany(group => group.Options).Select(option => option.ProductId).Distinct().ToArray();
        var products = await repository.GetByIdsWithRecipesAsync(tenantId, ids, cancellationToken);
        if (products.Count != ids.Length
            || products.Any(product => product.Id == comboProductId || !product.IsActive || product.Type != ProductType.Normal))
            return null;
        var productsById = products.ToDictionary(product => product.Id);
        if (groups!.SelectMany(group => group.Options).Any(option =>
                !productsById.TryGetValue(option.ProductId, out var product)
                || (option.ProductVariationId.HasValue
                    ? product.Variations.All(variation =>
                        variation.Id != option.ProductVariationId.Value || !variation.IsActive)
                    : product.Variations.Count > 0)))
            return null;
        return productsById;
    }
}

public sealed class SetProductStatusUseCase(
    IProductRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    ITableRealtimeNotifier? realtime = null) : ISetProductStatusUseCase
{
    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid tenantId, Guid productId, SetProductStatusRequest request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(tenantId, productId, cancellationToken);
        if (product is null) return Result<ProductDto>.Failure(Errors.ProductNotFound);
        if (!request.IsActive && await repository.IsUsedInComboAsync(tenantId, productId, cancellationToken))
            return Result<ProductDto>.Failure(Errors.ProductInUse);
        product.IsActive = request.IsActive;
        product.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (realtime is not null)
            await realtime.SalesDataInvalidatedAsync(tenantId, new(["products"], product.UpdatedAt), cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(product));
    }
}

public sealed class DeleteProductUseCase(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    ITableRealtimeNotifier? realtime = null,
    IFileStorage? fileStorage = null) : IDeleteProductUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(tenantId, productId, cancellationToken);
        if (product is null) return Result.Failure(Errors.ProductNotFound);
        if (await repository.IsUsedInComboAsync(tenantId, productId, cancellationToken))
            return Result.Failure(Errors.ProductInUse);
        repository.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (fileStorage is not null && ImageHelper.IsManagedReference(product.ImagePath))
            await fileStorage.DeleteAsync(tenantId, product.ImagePath, cancellationToken);
        if (realtime is not null)
            await realtime.SalesDataInvalidatedAsync(tenantId, new(["products"], product.UpdatedAt), cancellationToken);
        return Result.Success();
    }
}
