using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Core.Suppliers;

public static class SupplierRules
{
    public record struct PreparedSupplierValues(
        string Name,
        string NormalizedName,
        string? CommercialName,
        string? Document,
        string? Email,
        string? Phone,
        string? Address);

    public static bool TryPrepare(
        string? name,
        string? commercialName,
        string? document,
        string? email,
        string? phone,
        string? address,
        out PreparedSupplierValues values)
    {
        values = default;
        if (string.IsNullOrWhiteSpace(name)) return false;

        var cleanName = SettingsDefaults.Normalize(name);
        if (cleanName.Length is 0 or > 160) return false;

        var cleanCommercial = CleanOptional(commercialName);
        if (cleanCommercial?.Length > 160) return false;

        var cleanDoc = CleanOptional(document);
        if (cleanDoc?.Length > 80) return false;

        var cleanEmail = CleanOptional(email);
        if (cleanEmail?.Length > 320) return false;

        var cleanPhone = CleanOptional(phone);
        if (cleanPhone?.Length > 50) return false;

        var cleanAddress = CleanOptional(address);
        if (cleanAddress?.Length > 500) return false;

        values = new PreparedSupplierValues(
            cleanName,
            cleanName.ToUpperInvariant(),
            cleanCommercial,
            cleanDoc,
            cleanEmail,
            cleanPhone,
            cleanAddress);
        return true;
    }

    public static SupplierDto ToDto(Supplier entity) => new(
        entity.Id,
        entity.Name,
        entity.CommercialName,
        entity.Document,
        entity.Email,
        entity.Phone,
        entity.Address,
        entity.IsActive,
        entity.CreatedByUserName,
        entity.LastModifiedByUserName,
        entity.CreatedAt,
        entity.UpdatedAt);

    public static SupplierLookupDto ToLookupDto(Supplier entity) => new(
        entity.Id,
        entity.Name,
        entity.CommercialName);

    private static string? CleanOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : SettingsDefaults.Normalize(value);
}
