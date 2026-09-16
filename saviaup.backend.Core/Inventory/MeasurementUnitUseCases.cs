using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Inventory;

public sealed class ListMeasurementUnitsUseCase(IMeasurementUnitRepository repository) : IListMeasurementUnitsUseCase
{
    public async Task<Result<PagedResponse<MeasurementUnitDto>>> ExecuteAsync(
        Guid tenantId, MeasurementUnitQueryRequest request, CancellationToken cancellationToken)
    {
        var page = await repository.GetPageAsync(tenantId, request, cancellationToken);
        var mapped = new PageData<MeasurementUnitDto>(page.Items.Select(InventoryRules.ToDto).ToArray(), page.TotalCount);
        return Result<PagedResponse<MeasurementUnitDto>>.Success(InventoryRules.ToPage(mapped, request.Page, request.PageSize));
    }
}

public sealed class CreateMeasurementUnitUseCase(
    IMeasurementUnitRepository repository, IDateTimeProvider clock, IUnitOfWork unitOfWork) : ICreateMeasurementUnitUseCase
{
    public async Task<Result<MeasurementUnitDto>> ExecuteAsync(
        Guid tenantId, CreateMeasurementUnitRequest request, CancellationToken cancellationToken)
    {
        if (!InventoryRules.TryPrepareUnit(request.Code, request.Name, out var values))
            return Result<MeasurementUnitDto>.Failure(Errors.Validation);
        if (await repository.CodeOrNameExistsAsync(tenantId, values.NormalizedCode, values.NormalizedName, null, cancellationToken))
            return Result<MeasurementUnitDto>.Failure(Errors.MeasurementUnitAlreadyExists);
        var now = clock.UtcNow;
        var unit = new MeasurementUnit
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = values.Code,
            NormalizedCode = values.NormalizedCode,
            Name = values.Name,
            NormalizedName = values.NormalizedName,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.AddAsync(unit, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<MeasurementUnitDto>.Success(InventoryRules.ToDto(unit));
    }
}

public sealed class UpdateMeasurementUnitUseCase(
    IMeasurementUnitRepository repository, IDateTimeProvider clock, IUnitOfWork unitOfWork) : IUpdateMeasurementUnitUseCase
{
    public async Task<Result<MeasurementUnitDto>> ExecuteAsync(
        Guid tenantId, Guid unitId, UpdateMeasurementUnitRequest request, CancellationToken cancellationToken)
    {
        if (!InventoryRules.TryPrepareUnit(request.Code, request.Name, out var values))
            return Result<MeasurementUnitDto>.Failure(Errors.Validation);
        var unit = await repository.GetByIdAsync(tenantId, unitId, cancellationToken);
        if (unit is null) return Result<MeasurementUnitDto>.Failure(Errors.MeasurementUnitNotFound);
        if (await repository.CodeOrNameExistsAsync(tenantId, values.NormalizedCode, values.NormalizedName, unitId, cancellationToken))
            return Result<MeasurementUnitDto>.Failure(Errors.MeasurementUnitAlreadyExists);
        unit.Code = values.Code; unit.NormalizedCode = values.NormalizedCode;
        unit.Name = values.Name; unit.NormalizedName = values.NormalizedName; unit.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<MeasurementUnitDto>.Success(InventoryRules.ToDto(unit));
    }
}

public sealed class SetMeasurementUnitStatusUseCase(
    IMeasurementUnitRepository repository, IDateTimeProvider clock, IUnitOfWork unitOfWork) : ISetMeasurementUnitStatusUseCase
{
    public async Task<Result<MeasurementUnitDto>> ExecuteAsync(
        Guid tenantId, Guid unitId, SetMeasurementUnitStatusRequest request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(tenantId, unitId, cancellationToken);
        if (unit is null) return Result<MeasurementUnitDto>.Failure(Errors.MeasurementUnitNotFound);
        unit.IsActive = request.IsActive; unit.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<MeasurementUnitDto>.Success(InventoryRules.ToDto(unit));
    }
}

public sealed class DeleteMeasurementUnitUseCase(IMeasurementUnitRepository repository, IUnitOfWork unitOfWork) : IDeleteMeasurementUnitUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid unitId, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(tenantId, unitId, cancellationToken);
        if (unit is null) return Result.Failure(Errors.MeasurementUnitNotFound);
        if (await repository.IsInUseAsync(tenantId, unitId, cancellationToken))
            return Result.Failure(Errors.MeasurementUnitInUse);
        repository.Remove(unit);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
