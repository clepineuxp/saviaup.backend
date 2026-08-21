using System.Globalization;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Settings;

public sealed class BusinessSettingsUseCase(ISettingsRepository repository, IDateTimeProvider clock, IUnitOfWork unitOfWork) : IBusinessSettingsUseCase
{
    public async Task<Result<BusinessSettingsDto>> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await repository.GetTenantForUpdateAsync(tenantId, cancellationToken) is null) return Result<BusinessSettingsDto>.Failure(Errors.TenantNotFound);
        return Result<BusinessSettingsDto>.Success(Map(await repository.GetParametersAsync(tenantId, cancellationToken)));
    }

    public async Task<Result<BusinessSettingsDto>> UpdateAsync(Guid tenantId, UpdateBusinessSettingsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TipMessage) || request.SuggestedTipPercentage is < 0 or > 100)
            return Result<BusinessSettingsDto>.Failure(Errors.Validation);
        var tenant = await repository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<BusinessSettingsDto>.Failure(Errors.TenantNotFound);
        var parameters = (await repository.GetParametersAsync(tenantId, cancellationToken)).ToDictionary(item => item.Key);
        var now = clock.UtcNow;
        var values = new Dictionary<string, (string Value, string Type)>
        {
            [SettingsDefaults.UsesTables] = (request.UsesTables.ToString().ToLowerInvariant(), "boolean"),
            [SettingsDefaults.DeliveryEnabled] = (request.DeliveryEnabled.ToString().ToLowerInvariant(), "boolean"),
            [SettingsDefaults.RequiresOpenCashRegister] = (request.RequiresOpenCashRegister.ToString().ToLowerInvariant(), "boolean"),
            [SettingsDefaults.ShowVoluntaryTip] = (request.ShowVoluntaryTip.ToString().ToLowerInvariant(), "boolean"),
            [SettingsDefaults.TipMessage] = (request.TipMessage.Trim(), "string"),
            [SettingsDefaults.SuggestedTipPercentage] = (request.SuggestedTipPercentage.ToString(CultureInfo.InvariantCulture), "integer")
        };
        var additions = new List<OrganizationParameter>();
        foreach (var (key, value) in values)
        {
            if (parameters.TryGetValue(key, out var parameter)) { parameter.Value = value.Value; parameter.ValueType = value.Type; parameter.UpdatedAt = now; }
            else additions.Add(new OrganizationParameter { Id = Guid.NewGuid(), TenantId = tenantId, Key = key, Value = value.Value, ValueType = value.Type, CreatedAt = now, UpdatedAt = now });
        }
        if (additions.Count > 0) await repository.AddParametersAsync(additions, cancellationToken);
        tenant.RequiresOpenCashRegister = request.RequiresOpenCashRegister;
        tenant.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BusinessSettingsDto>.Success(new BusinessSettingsDto(request.UsesTables, request.DeliveryEnabled, request.RequiresOpenCashRegister,
            request.ShowVoluntaryTip, request.TipMessage.Trim(), request.SuggestedTipPercentage));
    }

    private static BusinessSettingsDto Map(IReadOnlyCollection<OrganizationParameter> parameters)
    {
        var values = SettingsDefaults.CreateBusinessParameters(Guid.Empty, DateTimeOffset.MinValue).ToDictionary(item => item.Key, item => item.Value);
        foreach (var parameter in parameters) values[parameter.Key] = parameter.Value;
        return new BusinessSettingsDto(Bool(values[SettingsDefaults.UsesTables]), Bool(values[SettingsDefaults.DeliveryEnabled]),
            Bool(values[SettingsDefaults.RequiresOpenCashRegister]), Bool(values[SettingsDefaults.ShowVoluntaryTip]),
            values[SettingsDefaults.TipMessage], int.TryParse(values[SettingsDefaults.SuggestedTipPercentage], out var percent) ? percent : 10);
    }

    private static bool Bool(string value) => bool.TryParse(value, out var parsed) && parsed;
}
