using System.Text;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Inventory;

internal sealed record IngredientValues(string Name, string NormalizedName, string? Description, decimal MinimumStock);
internal sealed record MeasurementUnitValues(string Code, string NormalizedCode, string Name, string NormalizedName);

internal static class InventoryRules
{
    public const decimal MaximumStock = 999999999999999.999m;

    public static bool TryPrepareIngredient(string? name, string? description, decimal minimumStock, out IngredientValues values)
    {
        var cleanName = CleanWords(name);
        var cleanDescription = CleanOptional(description);
        values = new IngredientValues(cleanName, cleanName.ToUpperInvariant(), cleanDescription, minimumStock);
        return cleanName.Length is > 0 and <= 120
            && (cleanDescription?.Length ?? 0) <= 1000
            && minimumStock is >= 0 and <= MaximumStock;
    }

    public static bool TryPrepareUnit(string? code, string? name, out MeasurementUnitValues values)
    {
        var cleanCode = (code ?? string.Empty).Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();
        var cleanName = CleanWords(name);
        values = new MeasurementUnitValues(cleanCode, cleanCode.ToUpperInvariant(), cleanName, cleanName.ToUpperInvariant());
        return cleanCode.Length is > 0 and <= 20
            && cleanName.Length is > 0 and <= 120
            && cleanCode.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-');
    }

    public static IngredientDto ToDto(Ingredient ingredient) => new(
        ingredient.Id,
        ingredient.Name,
        ingredient.Description,
        new CategoryReferenceDto(ingredient.Category.Id, ingredient.Category.Name, ingredient.Category.IsInventoryTracked),
        ToDto(ingredient.MeasurementUnit),
        ingredient.MinimumStock,
        ingredient.CurrentStock,
        ingredient.CurrentStock < ingredient.MinimumStock,
        ingredient.Category.IsInventoryTracked,
        ingredient.IsActive,
        ingredient.CreatedAt,
        ingredient.UpdatedAt);

    public static MeasurementUnitDto ToDto(MeasurementUnit unit) => new(
        unit.Id, unit.Code, unit.Name, unit.IsActive, unit.CreatedAt, unit.UpdatedAt);

    public static InventoryMovementDto ToDto(InventoryMovement movement) => new(
        movement.Id,
        movement.IngredientId,
        movement.Ingredient.Name,
        ToDto(movement.Ingredient.MeasurementUnit),
        movement.Direction,
        movement.Reason,
        movement.Quantity,
        movement.StockBefore,
        movement.StockAfter,
        movement.Note,
        movement.CreatedByUserId,
        movement.CreatedAt);

    public static PagedResponse<T> ToPage<T>(PageData<T> page, int pageNumber, int pageSize)
        => new(page.Items, pageNumber, pageSize, page.TotalCount,
            page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)pageSize));

    private static string CleanWords(string? value)
        => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC).Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    public static string? CleanOptional(string? value)
    {
        var clean = value?.Trim();
        return string.IsNullOrWhiteSpace(clean) ? null : clean;
    }
}
