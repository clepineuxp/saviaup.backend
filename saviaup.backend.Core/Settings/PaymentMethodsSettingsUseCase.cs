using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Settings;

public sealed class PaymentMethodsSettingsUseCase(ISettingsRepository repository, IDateTimeProvider clock, IUnitOfWork unitOfWork) : IPaymentMethodsSettingsUseCase
{
    public async Task<Result<IReadOnlyCollection<PaymentMethodDto>>> ListAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken)
        => Result<IReadOnlyCollection<PaymentMethodDto>>.Success((await repository.GetPaymentMethodsAsync(tenantId, includeInactive, cancellationToken)).Select(Map).ToArray());

    public async Task<Result<PaymentMethodDto>> CreateAsync(Guid tenantId, SavePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<PaymentMethodDto>.Failure(Errors.Validation);
        var name = SettingsDefaults.Normalize(request.Name);
        var normalized = SettingsDefaults.NormalizeKey(name);
        if (await repository.PaymentMethodNameExistsAsync(tenantId, normalized, null, cancellationToken))
            return Result<PaymentMethodDto>.Failure(Errors.PaymentMethodAlreadyExists);
        var now = clock.UtcNow;
        var method = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            NormalizedName = normalized,
            IsIncludedInCashOpening = request.IsIncludedInCashOpening,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.AddPaymentMethodAsync(method, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PaymentMethodDto>.Success(Map(method));
    }

    public async Task<Result<PaymentMethodDto>> UpdateAsync(Guid tenantId, Guid paymentMethodId, SavePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<PaymentMethodDto>.Failure(Errors.Validation);
        var method = await repository.GetPaymentMethodAsync(tenantId, paymentMethodId, cancellationToken);
        if (method is null) return Result<PaymentMethodDto>.Failure(Errors.PaymentMethodNotFound);
        var name = SettingsDefaults.Normalize(request.Name); var normalized = SettingsDefaults.NormalizeKey(name);
        if (await repository.PaymentMethodNameExistsAsync(tenantId, normalized, paymentMethodId, cancellationToken))
            return Result<PaymentMethodDto>.Failure(Errors.PaymentMethodAlreadyExists);
        method.Name = name; method.NormalizedName = normalized; method.IsIncludedInCashOpening = request.IsIncludedInCashOpening; method.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PaymentMethodDto>.Success(Map(method));
    }

    public async Task<Result<PaymentMethodDto>> SetStatusAsync(Guid tenantId, Guid paymentMethodId, SetPaymentMethodStatusRequest request, CancellationToken cancellationToken)
    {
        var method = await repository.GetPaymentMethodAsync(tenantId, paymentMethodId, cancellationToken);
        if (method is null) return Result<PaymentMethodDto>.Failure(Errors.PaymentMethodNotFound);
        method.IsActive = request.IsActive; method.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PaymentMethodDto>.Success(Map(method));
    }

    public async Task<Result> DeleteAsync(Guid tenantId, Guid paymentMethodId, CancellationToken cancellationToken)
    {
        var method = await repository.GetPaymentMethodAsync(tenantId, paymentMethodId, cancellationToken);
        if (method is null) return Result.Failure(Errors.PaymentMethodNotFound);
        repository.RemovePaymentMethod(method); await unitOfWork.SaveChangesAsync(cancellationToken); return Result.Success();
    }

    private static PaymentMethodDto Map(PaymentMethod item) => new(item.Id, item.Name, item.IsIncludedInCashOpening, item.IsActive, item.CreatedAt, item.UpdatedAt);
}
