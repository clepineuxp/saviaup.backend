namespace SaviaUp.Backend.Shared.Constants;

public static class PermissionCodes
{
    public const string OrdersRead = "orders.read";
    public const string OrdersCreate = "orders.create";
    public const string OrdersCancel = "orders.cancel";
    public const string TablesRead = "tables.read";
    public const string TablesManage = "tables.manage";
    public const string TablesOperate = "tables.operate";
    public const string InventoryRead = "inventory.read";
    public const string InventoryManage = "inventory.manage";
    public const string InventoryStockRead = "inventory.stock.read";
    public const string InventoryIngredientsRead = "inventory.ingredients.read";
    public const string InventoryIngredientsManage = "inventory.ingredients.manage";
    public const string InventoryMovementsRead = "inventory.movements.read";
    public const string InventoryMovementsManage = "inventory.movements.manage";
    public const string InventoryComplementsRead = "inventory.complements.read";
    public const string InventoryComplementsManage = "inventory.complements.manage";
    public const string ProductsRead = "products.read";
    public const string ProductsManage = "products.manage";
    public const string CategoriesRead = "categories.read";
    public const string CategoriesManage = "categories.manage";
    public const string KitchenRead = "kitchen.read";
    public const string ReportsRead = "reports.read";
    public const string BillingManage = "billing.manage";
    public const string SettingsManage = "settings.manage";

    public static readonly IReadOnlyCollection<string> All =
    [
        OrdersRead, OrdersCreate, OrdersCancel, TablesRead, TablesManage, TablesOperate,
        InventoryRead, InventoryManage, InventoryStockRead,
        InventoryIngredientsRead, InventoryIngredientsManage,
        InventoryMovementsRead, InventoryMovementsManage,
        InventoryComplementsRead, InventoryComplementsManage,
        ProductsRead, ProductsManage, CategoriesRead, CategoriesManage,
        KitchenRead, ReportsRead, BillingManage, SettingsManage
    ];
}
