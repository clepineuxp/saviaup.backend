using System.Globalization;
using SaviaUp.Backend.Core.Categories;
using SaviaUp.Backend.Core.Products;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tables;

public sealed class GetTableSalesCatalogVersionUseCase(ISettingsRepository settings)
    : IGetTableSalesCatalogVersionUseCase
{
    public async Task<Result<TableSalesCatalogVersionDto>> ExecuteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var lastModifiedAt = await ReadVersionAsync(settings, tenantId, cancellationToken);
        return Result<TableSalesCatalogVersionDto>.Success(ToDto(tenantId, lastModifiedAt));
    }

    internal static async Task<DateTimeOffset> ReadVersionAsync(
        ISettingsRepository settings,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var value = await settings.GetParameterValueAsync(
            tenantId,
            OrganizationParameterKeys.SalesCatalogLastModifiedAt,
            cancellationToken);
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : DateTimeOffset.UnixEpoch;
    }

    internal static TableSalesCatalogVersionDto ToDto(Guid tenantId, DateTimeOffset lastModifiedAt)
        => new(tenantId, lastModifiedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), lastModifiedAt);
}

public sealed class SynchronizeTableSalesCatalogUseCase(
    ISettingsRepository settings,
    ICategoryRepository categories,
    IProductRepository products,
    IRestaurantTableRepository tables) : ISynchronizeTableSalesCatalogUseCase
{
    private const int MaximumConsistencyAttempts = 3;

    public async Task<Result<TableSalesCatalogSnapshotDto>> ExecuteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaximumConsistencyAttempts; attempt++)
        {
            var versionBefore = await GetTableSalesCatalogVersionUseCase.ReadVersionAsync(
                settings,
                tenantId,
                cancellationToken);
            var categoryEntities = await categories.GetForTenantAsync(
                tenantId,
                includeInactive: false,
                cancellationToken,
                onlyWithProducts: true);
            var productEntities = await products.GetSalesCatalogAsync(tenantId, cancellationToken);
            var areaEntities = await tables.GetOperationAreasAsync(tenantId, cancellationToken);
            var versionAfter = await GetTableSalesCatalogVersionUseCase.ReadVersionAsync(
                settings,
                tenantId,
                cancellationToken);

            if (versionBefore != versionAfter)
            {
                if (attempt < MaximumConsistencyAttempts - 1) continue;
                throw new InvalidOperationException(
                    "The sales catalog synchronization could not reach a consistent version.");
            }

            var version = GetTableSalesCatalogVersionUseCase.ToDto(tenantId, versionAfter);
            return Result<TableSalesCatalogSnapshotDto>.Success(new(
                tenantId,
                version.Version,
                version.LastModifiedAt,
                categoryEntities.Select(category => CategoryRules.ToDto(category)).ToArray(),
                productEntities.Select(ProductRules.ToDto).ToArray(),
                areaEntities.Select(area => new DiningAreaTablesDto(
                    TableRules.ToDto(area),
                    area.Tables.OrderBy(table => table.NormalizedName).Select(TableRules.ToDto).ToArray()))
                    .ToArray()));
        }

        throw new InvalidOperationException("The sales catalog synchronization could not be completed.");
    }
}
