using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Tables;

public sealed class ListDiningAreasUseCase(IDiningAreaRepository repository) : IListDiningAreasUseCase
{
    public async Task<Result<IReadOnlyCollection<DiningAreaDto>>> ExecuteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var areas = await repository.GetForTenantAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<DiningAreaDto>>.Success(areas.Select(TableRules.ToDto).ToArray());
    }
}

public sealed class CreateDiningAreaUseCase(
    IDiningAreaRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICreateDiningAreaUseCase
{
    public async Task<Result<DiningAreaDto>> ExecuteAsync(
        Guid tenantId,
        CreateDiningAreaRequest request,
        CancellationToken cancellationToken)
    {
        if (!TableRules.TryCleanName(request.Name, out var name, out var normalizedName))
            return Result<DiningAreaDto>.Failure(Errors.Validation);
        var order = request.Order == 0
            ? await repository.GetNextOrderAsync(tenantId, cancellationToken)
            : request.Order;
        if (await repository.NameExistsAsync(tenantId, normalizedName, null, cancellationToken)
            || await repository.OrderExistsAsync(tenantId, order, null, cancellationToken))
            return Result<DiningAreaDto>.Failure(Errors.DiningAreaAlreadyExists);

        var now = clock.UtcNow;
        var area = new DiningArea
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            NormalizedName = normalizedName,
            Order = order,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.AddAsync(area, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<DiningAreaDto>.Success(TableRules.ToDto(area));
    }
}

public sealed class UpdateDiningAreaUseCase(
    IDiningAreaRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IUpdateDiningAreaUseCase
{
    public async Task<Result<DiningAreaDto>> ExecuteAsync(
        Guid tenantId,
        Guid areaId,
        UpdateDiningAreaRequest request,
        CancellationToken cancellationToken)
    {
        if (!TableRules.TryCleanName(request.Name, out var name, out var normalizedName))
            return Result<DiningAreaDto>.Failure(Errors.Validation);
        var area = await repository.GetByIdAsync(tenantId, areaId, cancellationToken);
        if (area is null) return Result<DiningAreaDto>.Failure(Errors.DiningAreaNotFound);
        if (await repository.NameExistsAsync(tenantId, normalizedName, areaId, cancellationToken)
            || await repository.OrderExistsAsync(tenantId, request.Order, areaId, cancellationToken))
            return Result<DiningAreaDto>.Failure(Errors.DiningAreaAlreadyExists);

        area.Name = name;
        area.NormalizedName = normalizedName;
        area.Order = request.Order;
        area.IsActive = request.IsActive;
        area.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<DiningAreaDto>.Success(TableRules.ToDto(area));
    }
}

public sealed class ReorderDiningAreasUseCase(
    IDiningAreaRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IReorderDiningAreasUseCase
{
    public async Task<Result<IReadOnlyCollection<DiningAreaDto>>> ExecuteAsync(
        Guid tenantId,
        ReorderDiningAreasRequest request,
        CancellationToken cancellationToken)
    {
        var areas = await repository.GetForUpdateAsync(tenantId, cancellationToken);
        var requestedIds = request.AreaIds.ToArray();
        if (requestedIds.Length != areas.Count
            || requestedIds.Distinct().Count() != requestedIds.Length
            || requestedIds.ToHashSet().SetEquals(areas.Select(area => area.Id)) is false)
            return Result<IReadOnlyCollection<DiningAreaDto>>.Failure(Errors.Validation);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var temporaryBase = areas.Max(area => area.Order) + areas.Count + 1;
            foreach (var (area, index) in areas.Select((area, index) => (area, index)))
                area.Order = temporaryBase + index;
            await unitOfWork.SaveChangesAsync(transactionToken);

            var byId = areas.ToDictionary(area => area.Id);
            foreach (var (areaId, index) in requestedIds.Select((areaId, index) => (areaId, index)))
            {
                byId[areaId].Order = index + 1;
                byId[areaId].UpdatedAt = clock.UtcNow;
            }
            await unitOfWork.SaveChangesAsync(transactionToken);
            var result = requestedIds.Select(areaId => TableRules.ToDto(byId[areaId])).ToArray();
            return Result<IReadOnlyCollection<DiningAreaDto>>.Success(result);
        }, cancellationToken);
    }
}

public sealed class DeleteDiningAreaUseCase(
    IDiningAreaRepository repository,
    IUnitOfWork unitOfWork) : IDeleteDiningAreaUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid areaId, CancellationToken cancellationToken)
    {
        var area = await repository.GetByIdAsync(tenantId, areaId, cancellationToken);
        if (area is null) return Result.Failure(Errors.DiningAreaNotFound);
        if (await repository.IsInUseAsync(tenantId, areaId, cancellationToken))
            return Result.Failure(Errors.DiningAreaInUse);
        repository.Remove(area);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
