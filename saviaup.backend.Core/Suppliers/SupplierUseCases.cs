using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Suppliers;

public sealed class GetSuppliersUseCase(
    ISupplierRepository repository) : IGetSuppliersUseCase
{
    public async Task<Result<SupplierPageDto>> ExecuteAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var p = page < 1 ? 1 : page;
        var ps = pageSize is < 1 or > 100 ? 20 : pageSize;

        var pageData = await repository.GetPageAsync(tenantId, cleanSearch, isActive, p, ps, cancellationToken);
        var dtos = pageData.Items.Select(SupplierRules.ToDto).ToArray();
        var totalPages = (int)Math.Ceiling(pageData.TotalCount / (double)ps);

        return Result<SupplierPageDto>.Success(new SupplierPageDto(dtos, p, ps, pageData.TotalCount, totalPages));
    }
}

public sealed class GetSupplierLookupUseCase(
    ISupplierRepository repository) : IGetSupplierLookupUseCase
{
    public async Task<Result<IReadOnlyCollection<SupplierLookupDto>>> ExecuteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var suppliers = await repository.GetLookupAsync(tenantId, cancellationToken);
        var dtos = suppliers.Select(SupplierRules.ToLookupDto).ToArray();
        return Result<IReadOnlyCollection<SupplierLookupDto>>.Success(dtos);
    }
}

public sealed class CreateSupplierUseCase(
    ISupplierRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICreateSupplierUseCase
{
    public async Task<Result<SupplierDto>> ExecuteAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        if (!SupplierRules.TryPrepare(
                request.Name,
                request.CommercialName,
                request.Document,
                request.Email,
                request.Phone,
                request.Address,
                out var values))
        {
            return Result<SupplierDto>.Failure(Errors.Validation);
        }

        if (await repository.NameExistsAsync(tenantId, values.NormalizedName, null, cancellationToken))
        {
            return Result<SupplierDto>.Failure(Errors.SupplierNameAlreadyExists);
        }

        var now = clock.UtcNow;
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            CommercialName = values.CommercialName,
            Document = values.Document,
            Email = values.Email,
            Phone = values.Phone,
            Address = values.Address,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            LastModifiedByUserId = userId,
            LastModifiedByUserName = userName,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(supplier, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SupplierDto>.Success(SupplierRules.ToDto(supplier));
    }
}

public sealed class UpdateSupplierUseCase(
    ISupplierRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IUpdateSupplierUseCase
{
    public async Task<Result<SupplierDto>> ExecuteAsync(
        Guid tenantId,
        Guid supplierId,
        Guid userId,
        string userName,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        if (!SupplierRules.TryPrepare(
                request.Name,
                request.CommercialName,
                request.Document,
                request.Email,
                request.Phone,
                request.Address,
                out var values))
        {
            return Result<SupplierDto>.Failure(Errors.Validation);
        }

        var supplier = await repository.GetByIdAsync(tenantId, supplierId, cancellationToken);
        if (supplier is null) return Result<SupplierDto>.Failure(Errors.SupplierNotFound);

        if (await repository.NameExistsAsync(tenantId, values.NormalizedName, supplierId, cancellationToken))
        {
            return Result<SupplierDto>.Failure(Errors.SupplierNameAlreadyExists);
        }

        var now = clock.UtcNow;
        supplier.Name = values.Name;
        supplier.NormalizedName = values.NormalizedName;
        supplier.CommercialName = values.CommercialName;
        supplier.Document = values.Document;
        supplier.Email = values.Email;
        supplier.Phone = values.Phone;
        supplier.Address = values.Address;
        supplier.LastModifiedByUserId = userId;
        supplier.LastModifiedByUserName = userName;
        supplier.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SupplierDto>.Success(SupplierRules.ToDto(supplier));
    }
}

public sealed class SetSupplierStatusUseCase(
    ISupplierRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ISetSupplierStatusUseCase
{
    public async Task<Result<SupplierDto>> ExecuteAsync(
        Guid tenantId,
        Guid supplierId,
        Guid userId,
        string userName,
        SetSupplierStatusRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await repository.GetByIdAsync(tenantId, supplierId, cancellationToken);
        if (supplier is null) return Result<SupplierDto>.Failure(Errors.SupplierNotFound);

        var now = clock.UtcNow;
        supplier.IsActive = request.IsActive;
        supplier.LastModifiedByUserId = userId;
        supplier.LastModifiedByUserName = userName;
        supplier.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SupplierDto>.Success(SupplierRules.ToDto(supplier));
    }
}
