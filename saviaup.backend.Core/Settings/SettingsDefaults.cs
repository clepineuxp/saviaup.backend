using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Settings;

public static class SettingsDefaults
{
    public const string UsesTables = OrganizationParameterKeys.UsesTables;
    public const string DeliveryEnabled = OrganizationParameterKeys.DeliveryEnabled;
    public const string RequiresOpenCashRegister = OrganizationParameterKeys.RequiresOpenCashRegister;
    public const string EnableCustomSales = OrganizationParameterKeys.EnableCustomSales;
    public const string ShowVoluntaryTip = OrganizationParameterKeys.ShowVoluntaryTip;
    public const string TipMessage = OrganizationParameterKeys.TipMessage;
    public const string SuggestedTipPercentage = OrganizationParameterKeys.SuggestedTipPercentage;
    public const string EnableDigitalMenu = OrganizationParameterKeys.EnableDigitalMenu;
    public const string DigitalMenuSlug = OrganizationParameterKeys.DigitalMenuSlug;
    public const string DigitalMenuStyle = OrganizationParameterKeys.DigitalMenuStyle;
    public const string EnableOrderPrintZones = OrganizationParameterKeys.EnableOrderPrintZones;
    public const string LockExpenseFinancialFieldsAfterCreation = OrganizationParameterKeys.LockExpenseFinancialFieldsAfterCreation;
    public const string SalesCatalogLastModifiedAt = OrganizationParameterKeys.SalesCatalogLastModifiedAt;

    public const string DefaultMenuStyleJson = "{\"templateId\":\"bistro\",\"primaryColor\":\"#10b981\",\"accentColor\":\"#f59e0b\",\"backgroundColor\":\"#ffffff\",\"textColor\":\"#0f172a\",\"selectedButtonTextColor\":\"#ffffff\",\"fontFamily\":\"Inter\",\"welcomeMessage\":\"¡Bienvenidos! Descubre nuestra selección de platos.\",\"showImages\":true,\"headerAlignment\":\"left\",\"infoPlacement\":\"header\",\"logoPlacement\":\"header\"}";

    public static IReadOnlyCollection<OrganizationParameter> CreateBusinessParameters(Guid tenantId, DateTimeOffset now) =>
    [
        Parameter(tenantId, UsesTables, "true", "boolean", now),
        Parameter(tenantId, DeliveryEnabled, "false", "boolean", now),
        Parameter(tenantId, RequiresOpenCashRegister, "false", "boolean", now),
        Parameter(tenantId, EnableCustomSales, "false", "boolean", now),
        Parameter(tenantId, ShowVoluntaryTip, "true", "boolean", now),
        Parameter(tenantId, TipMessage, "Servicio Voluntario", "string", now),
        Parameter(tenantId, SuggestedTipPercentage, "10", "integer", now),
        Parameter(tenantId, EnableDigitalMenu, "false", "boolean", now),
        Parameter(tenantId, DigitalMenuSlug, "", "string", now),
        Parameter(tenantId, DigitalMenuStyle, DefaultMenuStyleJson, "json", now),
        Parameter(tenantId, EnableOrderPrintZones, "false", "boolean", now),
        Parameter(tenantId, LockExpenseFinancialFieldsAfterCreation, "true", "boolean", now),
        Parameter(tenantId, SalesCatalogLastModifiedAt, now.ToString("O"), "datetime", now)
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
