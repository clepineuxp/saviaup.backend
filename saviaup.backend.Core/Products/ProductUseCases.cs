using SaviaUp.Backend.Core.Common;
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
    IUnitOfWork unitOfWork) : ICreateProductUseCase
{
    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid tenantId, CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty
            || !ProductRules.TryPrepare(
                request.Type,
                request.Name,
                request.Description,
                request.ImageUrl,
                request.SalePrice,
                request.PreparationTimeMinutes,
                out var values))
            return Result<ProductDto>.Failure(Errors.Validation);

        var category = await categoryRepository.GetByIdAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result<ProductDto>.Failure(Errors.CategoryNotFound);

        var now = clock.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = category.Id,
            Category = category,
            Type = values.Type,
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            Description = values.Description,
            ImageUrl = values.ImageUrl,
            SalePrice = values.SalePrice,
            PreparationTimeMinutes = values.PreparationTimeMinutes,
            IsInventoryTracked = category.IsInventoryTracked && request.IsInventoryTracked,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(product));
    }
}

public sealed class UpdateProductUseCase(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IUpdateProductUseCase
{
    public async Task<Result<ProductDto>> ExecuteAsync(
        Guid tenantId, Guid productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty
            || !ProductRules.TryPrepare(
                request.Type,
                request.Name,
                request.Description,
                request.ImageUrl,
                request.SalePrice,
                request.PreparationTimeMinutes,
                out var values))
            return Result<ProductDto>.Failure(Errors.Validation);

        var product = await productRepository.GetByIdAsync(tenantId, productId, cancellationToken);
        if (product is null) return Result<ProductDto>.Failure(Errors.ProductNotFound);
        var category = await categoryRepository.GetByIdAsync(tenantId, request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result<ProductDto>.Failure(Errors.CategoryNotFound);

        product.CategoryId = category.Id;
        product.Category = category;
        product.Type = values.Type;
        product.Name = values.Name;
        product.NormalizedName = values.NormalizedName;
        product.Description = values.Description;
        product.ImageUrl = values.ImageUrl;
        product.SalePrice = values.SalePrice;
        product.PreparationTimeMinutes = values.PreparationTimeMinutes;
        product.IsInventoryTracked = category.IsInventoryTracked && request.IsInventoryTracked;
        product.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProductDto>.Success(ProductRules.ToDto(product));
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
