using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Core.Settings;

public static class SettingsDefaults
{
    public const string UsesTables = "business.usesTables";
    public const string DeliveryEnabled = "business.deliveryEnabled";
    public const string RequiresOpenCashRegister = "business.requiresOpenCashRegister";
    public const string EnableCustomSales = "business.enableCustomSales";
    public const string ShowVoluntaryTip = "business.showVoluntaryTip";
    public const string TipMessage = "business.tipMessage";
    public const string SuggestedTipPercentage = "business.suggestedTipPercentage";

    public static IReadOnlyCollection<OrganizationParameter> CreateBusinessParameters(Guid tenantId, DateTimeOffset now) =>
    [
        Parameter(tenantId, UsesTables, "true", "boolean", now),
        Parameter(tenantId, DeliveryEnabled, "false", "boolean", now),
        Parameter(tenantId, RequiresOpenCashRegister, "false", "boolean", now),
        Parameter(tenantId, EnableCustomSales, "false", "boolean", now),
        Parameter(tenantId, ShowVoluntaryTip, "true", "boolean", now),
        Parameter(tenantId, TipMessage, "Servicio Voluntario", "string", now),
        Parameter(tenantId, SuggestedTipPercentage, "10", "integer", now)
    ];

    public static IReadOnlyCollection<PaymentMethod> CreatePaymentMethods(Guid tenantId, DateTimeOffset now) =>
    [
        Payment(tenantId, "Efectivo", true, now),
        Payment(tenantId, "Tarjeta/Datafono", false, now),
        Payment(tenantId, "Transferencia", false, now)
    ];

    private static OrganizationParameter Parameter(Guid tenantId, string key, string value, string type, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Key = key,
        Value = value,
        ValueType = type,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static PaymentMethod Payment(Guid tenantId, string name, bool cashOpening, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        NormalizedName = Normalize(name),
        IsIncludedInCashOpening = cashOpening,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    public static string Normalize(string value) => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    public static string NormalizeKey(string value) => Normalize(value).ToUpperInvariant();
}
