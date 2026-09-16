using System.Text;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Core.Tables;

internal static class TableRules
{
    public const decimal MaximumTotal = 9999999999999999.99m;

    public static bool TryCleanName(string? value, out string name, out string normalizedName)
    {
        name = string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC).Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        normalizedName = name.ToUpperInvariant();
        return name.Length is > 0 and <= 120;
    }

    public static bool TryParseStatus(string? value, out TableStatus status)
        => Enum.TryParse(value?.Trim(), true, out status)
            && status is TableStatus.Available or TableStatus.Occupied or TableStatus.Disabled;

    public static bool TryParseShape(string? value, out TableShape shape)
    {
        var normalized = (value ?? string.Empty).Trim().Replace('-', '_').Replace(' ', '_').ToUpperInvariant();
        shape = normalized switch
        {
            "SQUARE" => TableShape.Square,
            "ROUND" => TableShape.Round,
            "RECTANGLE_HORIZONTAL" or "RECTANGLEHORIZONTAL" => TableShape.RectangleHorizontal,
            "RECTANGLE_VERTICAL" or "RECTANGLEVERTICAL" => TableShape.RectangleVertical,
            _ => TableShape.Square
        };
        return normalized is "SQUARE" or "ROUND" or "RECTANGLE_HORIZONTAL" or "RECTANGLEHORIZONTAL"
            or "RECTANGLE_VERTICAL" or "RECTANGLEVERTICAL";
    }

    public static string ShapeToContract(TableShape shape) => shape switch
    {
        TableShape.Square => "SQUARE",
        TableShape.Round => "ROUND",
        TableShape.RectangleHorizontal => "RECTANGLE_HORIZONTAL",
        TableShape.RectangleVertical => "RECTANGLE_VERTICAL",
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
    };

    public static DiningAreaDto ToDto(DiningArea area) => new(
        area.Id,
        area.Name,
        area.Order,
        area.IsActive,
        area.CreatedAt,
        area.UpdatedAt);

    public static RestaurantTableDto ToDto(RestaurantTable table) => new(
        table.Id,
        table.DiningAreaId,
        table.Name,
        table.Capacity,
        table.PositionX,
        table.PositionY,
        ShapeToContract(table.Shape),
        table.IsDelivery,
        table.IsCashRegister,
        table.Status.ToString().ToUpperInvariant(),
        table.ActiveOrderId,
        table.ActiveOrderTotal,
        table.OccupiedAt,
        table.CreatedAt,
        table.UpdatedAt);
}
