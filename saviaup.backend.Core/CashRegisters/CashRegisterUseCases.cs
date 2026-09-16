using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.CashRegisters;

public sealed class ListCashRegistersUseCase(
    ICashRegisterRepository repository,
    ICashRegisterShiftRepository shiftRepository) : IListCashRegistersUseCase
{
    public async Task<Result<IReadOnlyCollection<CashRegisterDto>>> ExecuteAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var cashRegisters = await repository.GetForTenantAsync(tenantId, includeInactive, cancellationToken);
        var dtos = new List<CashRegisterDto>();

        foreach (var cr in cashRegisters)
        {
            var openShift = await shiftRepository.GetOpenShiftAsync(tenantId, cr.Id, cancellationToken);
            dtos.Add(new CashRegisterDto(
                cr.Id,
                cr.Name,
                cr.Location,
                cr.IsActive,
                openShift is not null,
                openShift?.Id,
                cr.CreatedAt,
                cr.UpdatedAt));
        }

        return Result<IReadOnlyCollection<CashRegisterDto>>.Success(dtos);
    }
}

public sealed class CreateCashRegisterUseCase(
    ICashRegisterRepository repository,
    ICashRegisterShiftRepository shiftRepository,
    IUnitOfWork unitOfWork) : ICreateCashRegisterUseCase
{
    public async Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        CreateCashRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
        {
            return Result<CashRegisterDto>.Failure(Errors.Validation);
        }

        var location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        if (location?.Length > 200)
        {
            return Result<CashRegisterDto>.Failure(Errors.Validation);
        }

        var normalizedName = name.ToUpperInvariant();
        if (await repository.NameExistsAsync(tenantId, normalizedName, null, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterNameAlreadyExists);
        }

        if (request.IsActive && await repository.HasOtherActiveAsync(tenantId, null, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterSingleActiveExceeded);
        }

        var now = DateTimeOffset.UtcNow;
        var cashRegister = new CashRegister
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            NormalizedName = normalizedName,
            Location = location,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(cashRegister, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var openShift = await shiftRepository.GetOpenShiftAsync(tenantId, cashRegister.Id, cancellationToken);
        return Result<CashRegisterDto>.Success(new CashRegisterDto(
            cashRegister.Id,
            cashRegister.Name,
            cashRegister.Location,
            cashRegister.IsActive,
            openShift is not null,
            openShift?.Id,
            cashRegister.CreatedAt,
            cashRegister.UpdatedAt));
    }
}

public sealed class UpdateCashRegisterUseCase(
    ICashRegisterRepository repository,
    ICashRegisterShiftRepository shiftRepository,
    IUnitOfWork unitOfWork) : IUpdateCashRegisterUseCase
{
    public async Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        Guid cashRegisterId,
        UpdateCashRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var cashRegister = await repository.GetByIdAsync(tenantId, cashRegisterId, cancellationToken);
        if (cashRegister is null)
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterNotFound);
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
        {
            return Result<CashRegisterDto>.Failure(Errors.Validation);
        }

        var location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        if (location?.Length > 200)
        {
            return Result<CashRegisterDto>.Failure(Errors.Validation);
        }

        var normalizedName = name.ToUpperInvariant();
        if (await repository.NameExistsAsync(tenantId, normalizedName, cashRegisterId, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterNameAlreadyExists);
        }

        if (request.IsActive && await repository.HasOtherActiveAsync(tenantId, cashRegisterId, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterSingleActiveExceeded);
        }

        cashRegister.Name = name;
        cashRegister.NormalizedName = normalizedName;
        cashRegister.Location = location;
        cashRegister.IsActive = request.IsActive;
        cashRegister.UpdatedAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var openShift = await shiftRepository.GetOpenShiftAsync(tenantId, cashRegister.Id, cancellationToken);
        return Result<CashRegisterDto>.Success(new CashRegisterDto(
            cashRegister.Id,
            cashRegister.Name,
            cashRegister.Location,
            cashRegister.IsActive,
            openShift is not null,
            openShift?.Id,
            cashRegister.CreatedAt,
            cashRegister.UpdatedAt));
    }
}

public sealed class SetCashRegisterStatusUseCase(
    ICashRegisterRepository repository,
    ICashRegisterShiftRepository shiftRepository,
    IUnitOfWork unitOfWork) : ISetCashRegisterStatusUseCase
{
    public async Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        Guid cashRegisterId,
        SetCashRegisterStatusRequest request,
        CancellationToken cancellationToken)
    {
        var cashRegister = await repository.GetByIdAsync(tenantId, cashRegisterId, cancellationToken);
        if (cashRegister is null)
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterNotFound);
        }

        if (request.IsActive && await repository.HasOtherActiveAsync(tenantId, cashRegisterId, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterSingleActiveExceeded);
        }

        cashRegister.IsActive = request.IsActive;
        cashRegister.UpdatedAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var openShift = await shiftRepository.GetOpenShiftAsync(tenantId, cashRegister.Id, cancellationToken);
        return Result<CashRegisterDto>.Success(new CashRegisterDto(
            cashRegister.Id,
            cashRegister.Name,
            cashRegister.Location,
            cashRegister.IsActive,
            openShift is not null,
            openShift?.Id,
            cashRegister.CreatedAt,
            cashRegister.UpdatedAt));
    }
}

public sealed class DeleteCashRegisterUseCase(ICashRegisterRepository repository, IUnitOfWork unitOfWork) : IDeleteCashRegisterUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid cashRegisterId, CancellationToken cancellationToken)
    {
        var cashRegister = await repository.GetByIdAsync(tenantId, cashRegisterId, cancellationToken);
        if (cashRegister is null)
        {
            return Result.Failure(Errors.CashRegisterNotFound);
        }

        if (await repository.IsInUseAsync(tenantId, cashRegisterId, cancellationToken))
        {
            return Result.Failure(Errors.CashRegisterInUse);
        }

        repository.Remove(cashRegister);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
