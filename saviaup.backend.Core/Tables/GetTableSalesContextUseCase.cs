using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tables;

/// <summary>
/// Deliberately exposes only the configuration required while taking orders.
/// Administrative organization and access data stay behind their settings endpoints.
/// </summary>
public sealed class GetTableSalesContextUseCase(
    ITenantRepository tenants,
    IBusinessSettingsUseCase business,
    IPaymentMethodsSettingsUseCase payments,
    IPermissionService permissions) : IGetTableSalesContextUseCase
{
    public async Task<Result<TableSalesContextDto>> ExecuteAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<TableSalesContextDto>.Failure(Errors.TenantNotFound);

        var businessResult = await business.GetAsync(tenantId, cancellationToken);
        if (!businessResult.IsSuccess) return Result<TableSalesContextDto>.Failure(businessResult.Error!);

        var paymentResult = await payments.ListAsync(tenantId, false, cancellationToken);
        if (!paymentResult.IsSuccess) return Result<TableSalesContextDto>.Failure(paymentResult.Error!);

        var canRead = await permissions.IsAllowedAsync(tenantId, roleId, PermissionCodes.TablesRead, cancellationToken);
        var canOperate = await permissions.IsAllowedAsync(tenantId, roleId, PermissionCodes.TablesOperate, cancellationToken);
        var canManage = await permissions.IsAllowedAsync(tenantId, roleId, PermissionCodes.TablesManage, cancellationToken);

        return Result<TableSalesContextDto>.Success(new(
            new TableSalesOrganizationDto(tenant.Name, tenant.LogoData is not null, tenant.UpdatedAt.ToUnixTimeMilliseconds()),
            new TableSalesBusinessDto(
                businessResult.Value!.EnableCustomSales,
                businessResult.Value.ShowVoluntaryTip,
                businessResult.Value.SuggestedTipPercentage),
            paymentResult.Value!.Select(method => new TableSalesPaymentMethodDto(method.Id, method.Name)).ToArray(),
            new TableSalesCapabilitiesDto(canRead, canOperate, canManage)));
    }
}
