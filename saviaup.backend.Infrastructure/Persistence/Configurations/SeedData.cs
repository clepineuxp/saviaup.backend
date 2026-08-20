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
        (Guid.Parse("10000000-0000-0000-0000-000000000008"), "settings", "Settings")
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
        Permission("20000000-0000-0000-0000-000000000013", 7, PermissionCodes.SettingsManage)
    ];

    private static Permission Permission(string id, int moduleIndex, string code) => new()
    {
        Id = Guid.Parse(id),
        ModuleId = ModuleDefinitions[moduleIndex].Id,
        Code = code,
        Description = code
    };
}
