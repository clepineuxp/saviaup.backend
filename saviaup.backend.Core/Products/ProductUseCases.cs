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
    IStoredImageRepository? imageRepository = null) : ICreateProductUseCase
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
                out var values))
            return Result<ProductDto>.Failure(Errors.Validation);

        var category = await categoryRepository.GetByIdAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result<ProductDto>.Failure(Errors.CategoryNotFound);

        var now = clock.UtcNow;
        var productId = Guid.NewGuid();
        Guid? imageRef = null;
        StoredImage? imageStored = null;

        if (imageRepository != null
            && !string.IsNullOrWhiteSpace(values.Image)
            && values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            imageStored = ImageHelper.CreateStoredImage(tenantId, "products", productId.ToString(), values.Image, now);
            if (imageStored != null)
            {
                await imageRepository.AddAsync(imageStored, cancellationToken);
                imageRef = imageStored.Id;
            }
        }

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
            ImageRef = imageRef,
            ImageStored = imageStored,
            SalePrice = values.SalePrice,
            PreparationTimeMinutes = values.PreparationTimeMinutes,
            IsInventoryTracked = category.IsInventoryTracked && request.IsInventoryTracked,
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
                if (string.IsNullOrWhiteSpace(vName) || v.SalePrice <= 0) continue;
                product.Variations.Add(new ProductVariation
                {
                    Id = v.Id.HasValue && v.Id.Value != Guid.Empty ? v.Id.Value : Guid.NewGuid(),
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

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var reloaded = await productRepository.GetByIdAsync(tenantId, productId, cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(reloaded ?? product));
    }
}

public sealed class UpdateProductUseCase(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    IStoredImageRepository? imageRepository = null) : IUpdateProductUseCase
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
                out var values))
            return Result<ProductDto>.Failure(Errors.Validation);

        // GetByIdForUpdateAsync carga el producto SIN RecipeItems en el ChangeTracker.
        // Esto evita el DbUpdateConcurrencyException que ocurría al intentar gestionar
        // los RecipeItems antiguos (ya rastreados) junto con los nuevos en el mismo contexto.
        var product = await productRepository.GetByIdForUpdateAsync(tenantId, productId, cancellationToken);
        if (product is null) return Result<ProductDto>.Failure(Errors.ProductNotFound);

        // AsNoTracking porque GetByIdForUpdateAsync ya tiene product.Category en el tracker.
        var category = await categoryRepository.GetByIdAsNoTrackingAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result<ProductDto>.Failure(Errors.CategoryNotFound);

        var now = clock.UtcNow;

        if (imageRepository != null
            && !string.IsNullOrWhiteSpace(values.Image)
            && values.Image.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            if (product.ImageStored == null || product.ImageStored.Base64Content != values.Image)
            {
                var storedImage = ImageHelper.CreateStoredImage(tenantId, "products", productId.ToString(), values.Image, now);
                if (storedImage != null)
                {
                    await imageRepository.AddAsync(storedImage, cancellationToken);
                    product.ImageRef = storedImage.Id;
                    product.ImageStored = storedImage;
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(values.Image))
        {
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
        product.IsInventoryTracked = category.IsInventoryTracked && request.IsInventoryTracked;
        product.LastModifiedByUserId = userId;
        product.LastModifiedByUserName = userName;
        product.UpdatedAt = now;

        if (request.Recipe is not null)
        {
            // DeleteRecipeItemsAsync usa ExecuteDeleteAsync → bypass del ChangeTracker.
            await productRepository.DeleteRecipeItemsAsync(tenantId, productId, cancellationToken);

            var itemsToAdd = new List<ProductRecipeItem>();
            var orderIdx = 0;
            foreach (var r in request.Recipe)
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

        if (request.Variations is not null)
        {
            await productRepository.DeleteVariationsAsync(tenantId, productId, cancellationToken);

            var varsToAdd = new List<ProductVariation>();
            var varIdx = 0;
            foreach (var v in request.Variations)
            {
                var vName = v.Name?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(vName) || v.SalePrice <= 0) continue;
                varsToAdd.Add(new ProductVariation
                {
                    Id = v.Id.HasValue && v.Id.Value != Guid.Empty ? v.Id.Value : Guid.NewGuid(),
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

            if (varsToAdd.Count > 0)
            {
                await productRepository.AddVariationsAsync(varsToAdd, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var reloaded = await productRepository.GetByIdAsync(tenantId, product.Id, cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(reloaded ?? product));
    }
}

public sealed class SetProductStatusUseCase(
    IProductRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ISetProductStatusUseCase
{
    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid tenantId, Guid productId, SetProductStatusRequest request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(tenantId, productId, cancellationToken);
        if (product is null) return Result<ProductDto>.Failure(Errors.ProductNotFound);
        product.IsActive = request.IsActive;
        product.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(product));
    }
}

public sealed class DeleteProductUseCase(IProductRepository repository, IUnitOfWork unitOfWork) : IDeleteProductUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(tenantId, productId, cancellationToken);
        if (product is null) return Result.Failure(Errors.ProductNotFound);
        repository.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
