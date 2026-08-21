using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.CashRegisters;

public sealed class ListCashRegistersUseCase : IListCashRegistersUseCase
{
    private readonly ICashRegisterRepository _repository;

    public ListCashRegistersUseCase(ICashRegisterRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<CashRegisterDto>>> ExecuteAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var cashRegisters = await _repository.GetForTenantAsync(tenantId, includeInactive, cancellationToken);
        var dtos = cashRegisters
            .Select(cr => new CashRegisterDto(cr.Id, cr.Name, cr.Location, cr.IsActive, cr.CreatedAt, cr.UpdatedAt))
            .ToArray();
        return Result<IReadOnlyCollection<CashRegisterDto>>.Success(dtos);
    }
}

public sealed class CreateCashRegisterUseCase : ICreateCashRegisterUseCase
{
    private readonly ICashRegisterRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCashRegisterUseCase(ICashRegisterRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

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
        if (await _repository.NameExistsAsync(tenantId, normalizedName, null, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterNameAlreadyExists);
        }

        if (request.IsActive && await _repository.HasOtherActiveAsync(tenantId, null, cancellationToken))
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

        await _repository.AddAsync(cashRegister, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CashRegisterDto>.Success(new CashRegisterDto(
            cashRegister.Id,
            cashRegister.Name,
            cashRegister.Location,
            cashRegister.IsActive,
            cashRegister.CreatedAt,
            cashRegister.UpdatedAt));
    }
}

public sealed class UpdateCashRegisterUseCase : IUpdateCashRegisterUseCase
{
    private readonly ICashRegisterRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCashRegisterUseCase(ICashRegisterRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        Guid cashRegisterId,
        UpdateCashRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var cashRegister = await _repository.GetByIdAsync(tenantId, cashRegisterId, cancellationToken);
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
        if (await _repository.NameExistsAsync(tenantId, normalizedName, cashRegisterId, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterNameAlreadyExists);
        }

        if (request.IsActive && await _repository.HasOtherActiveAsync(tenantId, cashRegisterId, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterSingleActiveExceeded);
        }

        cashRegister.Name = name;
        cashRegister.NormalizedName = normalizedName;
        cashRegister.Location = location;
        cashRegister.IsActive = request.IsActive;
        cashRegister.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CashRegisterDto>.Success(new CashRegisterDto(
            cashRegister.Id,
            cashRegister.Name,
            cashRegister.Location,
            cashRegister.IsActive,
            cashRegister.CreatedAt,
            cashRegister.UpdatedAt));
    }
}

public sealed class SetCashRegisterStatusUseCase : ISetCashRegisterStatusUseCase
{
    private readonly ICashRegisterRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetCashRegisterStatusUseCase(ICashRegisterRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        Guid cashRegisterId,
        SetCashRegisterStatusRequest request,
        CancellationToken cancellationToken)
    {
        var cashRegister = await _repository.GetByIdAsync(tenantId, cashRegisterId, cancellationToken);
        if (cashRegister is null)
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterNotFound);
        }

        if (request.IsActive && await _repository.HasOtherActiveAsync(tenantId, cashRegisterId, cancellationToken))
        {
            return Result<CashRegisterDto>.Failure(Errors.CashRegisterSingleActiveExceeded);
        }

        cashRegister.IsActive = request.IsActive;
        cashRegister.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CashRegisterDto>.Success(new CashRegisterDto(
            cashRegister.Id,
            cashRegister.Name,
            cashRegister.Location,
            cashRegister.IsActive,
            cashRegister.CreatedAt,
            cashRegister.UpdatedAt));
    }
}

public sealed class DeleteCashRegisterUseCase : IDeleteCashRegisterUseCase
{
    private readonly ICashRegisterRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCashRegisterUseCase(ICashRegisterRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid tenantId, Guid cashRegisterId, CancellationToken cancellationToken)
    {
        var cashRegister = await _repository.GetByIdAsync(tenantId, cashRegisterId, cancellationToken);
        if (cashRegister is null)
        {
            return Result.Failure(Errors.CashRegisterNotFound);
        }

        if (await _repository.IsInUseAsync(tenantId, cashRegisterId, cancellationToken))
        {
            return Result.Failure(Errors.CashRegisterInUse);
        }

        _repository.Remove(cashRegister);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
