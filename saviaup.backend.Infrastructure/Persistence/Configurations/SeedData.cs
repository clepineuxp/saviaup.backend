using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Shared.Constants;
using DomainModule = SaviaUp.Backend.Domain.Entities.Module;

namespace SaviaUp.Backend.Infrastructure.Persistence.Configurations;

internal static class SeedData
{
    private static readonly (Guid Id, string Code, string Name)[] ModuleDefinitions =
    [
        (Guid.Parse("10000000-0000-0000-0000-000000000001"), "orders", "Orders"),
        (Guid.Parse("10000000-0000-0000-0000-000000000002"), "tables", "Tables"),
        (Guid.Parse("10000000-0000-0000-0000-000000000003"), "inventory", "Inventory"),
        (Guid.Parse("10000000-0000-0000-0000-000000000004"), "products", "Products"),
        (Guid.Parse("10000000-0000-0000-0000-000000000005"), "kitchen", "Kitchen"),
        (Guid.Parse("10000000-0000-0000-0000-000000000006"), "reports", "Reports"),
        (Guid.Parse("10000000-0000-0000-0000-000000000007"), "billing", "Billing"),
        (Guid.Parse("10000000-0000-0000-0000-000000000008"), "settings", "Settings"),
        (Guid.Parse("10000000-0000-0000-0000-000000000009"), "categories", "Categories")
    ];

    public static readonly DomainModule[] Modules = ModuleDefinitions
        .Select(value => new DomainModule { Id = value.Id, Code = value.Code, Name = value.Name, IsActive = true })
        .ToArray();

    public static readonly Permission[] Permissions =
    [
        Permission("20000000-0000-0000-0000-000000000001", 0, PermissionCodes.OrdersRead),
        Permission("20000000-0000-0000-0000-000000000002", 0, PermissionCodes.OrdersCreate),
        Permission("20000000-0000-0000-0000-000000000003", 0, PermissionCodes.OrdersCancel),
        Permission("20000000-0000-0000-0000-000000000004", 1, PermissionCodes.TablesRead),
        Permission("20000000-0000-0000-0000-000000000005", 1, PermissionCodes.TablesManage),
        Permission("20000000-0000-0000-0000-000000000006", 2, PermissionCodes.InventoryRead),
        Permission("20000000-0000-0000-0000-000000000007", 2, PermissionCodes.InventoryManage),
        Permission("20000000-0000-0000-0000-000000000008", 3, PermissionCodes.ProductsRead),
        Permission("20000000-0000-0000-0000-000000000009", 3, PermissionCodes.ProductsManage),
        Permission("20000000-0000-0000-0000-000000000010", 4, PermissionCodes.KitchenRead),
        Permission("20000000-0000-0000-0000-000000000011", 5, PermissionCodes.ReportsRead),
        Permission("20000000-0000-0000-0000-000000000012", 6, PermissionCodes.BillingManage),
        Permission("20000000-0000-0000-0000-000000000013", 7, PermissionCodes.SettingsManage),
        Permission("20000000-0000-0000-0000-000000000014", 8, PermissionCodes.CategoriesRead),
        Permission("20000000-0000-0000-0000-000000000015", 8, PermissionCodes.CategoriesManage),
        Permission("20000000-0000-0000-0000-000000000016", 2, PermissionCodes.InventoryStockRead),
        Permission("20000000-0000-0000-0000-000000000017", 2, PermissionCodes.InventoryIngredientsRead),
        Permission("20000000-0000-0000-0000-000000000018", 2, PermissionCodes.InventoryIngredientsManage),
        Permission("20000000-0000-0000-0000-000000000019", 2, PermissionCodes.InventoryMovementsRead),
        Permission("20000000-0000-0000-0000-000000000020", 2, PermissionCodes.InventoryMovementsManage),
        Permission("20000000-0000-0000-0000-000000000021", 2, PermissionCodes.InventoryComplementsRead),
        Permission("20000000-0000-0000-0000-000000000022", 2, PermissionCodes.InventoryComplementsManage),
        Permission("20000000-0000-0000-0000-000000000023", 1, PermissionCodes.TablesOperate),
        Permission("20000000-0000-0000-0000-000000000024", 7, PermissionCodes.SettingsOrganizationRead),
        Permission("20000000-0000-0000-0000-000000000025", 7, PermissionCodes.SettingsOrganizationManage),
        Permission("20000000-0000-0000-0000-000000000026", 7, PermissionCodes.SettingsBusinessRead),
        Permission("20000000-0000-0000-0000-000000000027", 7, PermissionCodes.SettingsBusinessManage),
        Permission("20000000-0000-0000-0000-000000000028", 7, PermissionCodes.SettingsPaymentMethodsRead),
        Permission("20000000-0000-0000-0000-000000000029", 7, PermissionCodes.SettingsPaymentMethodsManage),
        Permission("20000000-0000-0000-0000-000000000030", 7, PermissionCodes.SettingsUsersRead),
        Permission("20000000-0000-0000-0000-000000000031", 7, PermissionCodes.SettingsUsersManage),
        Permission("20000000-0000-0000-0000-000000000032", 7, PermissionCodes.SettingsRolesRead),
        Permission("20000000-0000-0000-0000-000000000033", 7, PermissionCodes.SettingsRolesManage)
    ];

    private static Permission Permission(string id, int moduleIndex, string code) => new()
    {
        Id = Guid.Parse(id),
        ModuleId = ModuleDefinitions[moduleIndex].Id,
        Code = code,
        Description = code
    };
}
