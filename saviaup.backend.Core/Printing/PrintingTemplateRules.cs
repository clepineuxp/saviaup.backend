using System.Text.Json;
using SaviaUp.Backend.Domain.DTOs;

namespace SaviaUp.Backend.Core.Printing;

public static class PrintingTemplateRules
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PrintingTemplateSettingsDto Default { get; } = new(new(), new());

    public static string DefaultJson { get; } = JsonSerializer.Serialize(Default, JsonOptions);

    public static PrintingTemplateSettingsDto Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Default;
        try
        {
            var parsed = JsonSerializer.Deserialize<PrintingTemplateSettingsDto>(value, JsonOptions);
            return parsed is null ? Default : Normalize(parsed);
        }
        catch (JsonException)
        {
            return Default;
        }
    }

    public static string Serialize(PrintingTemplateSettingsDto value)
        => JsonSerializer.Serialize(Normalize(value), JsonOptions);

    public static PrintingTemplateSettingsDto Normalize(PrintingTemplateSettingsDto value)
    {
        var receipt = value.Receipt ?? new ReceiptPrintTemplateDto();
        var kitchen = value.Kitchen ?? new KitchenPrintTemplateDto();
        return new PrintingTemplateSettingsDto(
            receipt with
            {
                PaperWidthMm = receipt.PaperWidthMm == 58 ? 58 : 80,
                BaseFontSize = Math.Clamp(receipt.BaseFontSize, 8, 18),
                HeaderFontSize = Math.Clamp(receipt.HeaderFontSize, 10, 28),
                ItemFontSize = Math.Clamp(receipt.ItemFontSize, 8, 22),
                TotalFontSize = Math.Clamp(receipt.TotalFontSize, 10, 28),
                VoluntaryTipFontSize = Math.Clamp(receipt.VoluntaryTipFontSize, 8, 24),
                VoluntaryTipAlignment = Choice(receipt.VoluntaryTipAlignment, "LEFT", "CENTER", "RIGHT"),
                VoluntaryTipPosition = Choice(receipt.VoluntaryTipPosition, "BEFORE_TOTAL", "AFTER_TOTAL"),
                LogoWidthMm = Math.Clamp(receipt.LogoWidthMm, 20, 72)
            },
            kitchen with
            {
                HeaderFontScale = Math.Clamp(kitchen.HeaderFontScale, 1, 2),
                MetadataFontScale = Math.Clamp(kitchen.MetadataFontScale, 1, 2),
                ItemFontScale = Math.Clamp(kitchen.ItemFontScale, 1, 2),
                NotesFontScale = Math.Clamp(kitchen.NotesFontScale, 1, 2),
                HeaderAlignment = Choice(kitchen.HeaderAlignment, "CENTER", "LEFT", "RIGHT"),
                Layout = Choice(kitchen.Layout, "STANDARD", "COMPACT", "SPACIOUS"),
                MaxItemNameLines = Math.Clamp(kitchen.MaxItemNameLines, 1, 3)
            });
    }

    private static string Choice(string? value, string fallback, params string[] allowed)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized is not null && allowed.Contains(normalized, StringComparer.Ordinal)
            ? normalized
            : fallback;
    }
}
