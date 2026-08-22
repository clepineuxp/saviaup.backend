using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class CriticalEndpointsTests(SaviaUpApiFactory factory) : IClassFixture<SaviaUpApiFactory>
{
    [Fact]
    public async Task Register_Login_Refresh_And_Tenant_Flow_Works_End_To_End()
    {
        using var client = factory.CreateClient();
        var email = $"integration-{Guid.NewGuid():N}@saviaup.test";
        const string password = "Secure123!*";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Ana",
            lastName = "Prueba",
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var registerJson = await register.Content.ReadFromJsonAsync<JsonElement>();
        var initialAccessToken = registerJson.GetProperty("session").GetProperty("accessToken").GetString();
        var initialRefreshToken = registerJson.GetProperty("session").GetProperty("refreshToken").GetString();
        Assert.True(registerJson.GetProperty("session").GetProperty("requiresTenantSelection").GetBoolean());

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = initialRefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var refreshJson = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        var refreshedAccessToken = refreshJson.GetProperty("accessToken").GetString();
        Assert.NotEqual(initialRefreshToken, refreshJson.GetProperty("refreshToken").GetString());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
        var emptyTenants = await client.GetAsync("/api/tenants");
        Assert.Equal(HttpStatusCode.OK, emptyTenants.StatusCode);
        Assert.Empty((await emptyTenants.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

        var create = await client.PostAsJsonAsync("/api/tenants", new { name = "Secret Garden" });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var createJson = await create.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = createJson.GetProperty("tenant").GetProperty("id").GetGuid();
        var contextualAccessToken = createJson.GetProperty("tokens").GetProperty("accessToken").GetString();
        Assert.Equal("Secret Garden", createJson.GetProperty("tenant").GetProperty("name").GetString());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", contextualAccessToken);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-CO");

        var modules = await client.GetAsync("/api/modules/available");
        Assert.Equal(HttpStatusCode.OK, modules.StatusCode);
        var modulesJson = await modules.Content.ReadFromJsonAsync<JsonElement>();
        var sections = modulesJson.GetProperty("sections").EnumerateArray().ToArray();
        Assert.Equal(4, sections.Length);
        Assert.Equal(10, sections.Sum(section => section.GetProperty("modules").GetArrayLength()));
        var options = sections.SelectMany(section => section.GetProperty("options").EnumerateArray()).ToArray();
        Assert.Equal(2, options.Length);
        Assert.Contains(options, option => option.GetProperty("code").GetString() == "tables.manage");
        Assert.Contains(options, option => option.GetProperty("code").GetString() == "cash-registers.manage");
        Assert.Equal([1, 2, 3, 4], sections.Select(section => section.GetProperty("order").GetInt32()));
        Assert.False(sections[0].GetProperty("isGrouped").GetBoolean());
        Assert.True(sections[1].GetProperty("isGrouped").GetBoolean());
        Assert.Equal(
            ["orders", "reports", "billing", "cash_registers"],
            sections[1].GetProperty("modules").EnumerateArray().Select(module => module.GetProperty("code").GetString()));
        Assert.Contains(
            sections[2].GetProperty("modules").EnumerateArray(),
            module => module.GetProperty("code").GetString() == "categories"
                && module.GetProperty("name").GetString() == "Categorías"
                && module.GetProperty("order").GetInt32() == 2);
        Assert.Equal(JsonValueKind.Null, modulesJson.GetProperty("emptyStateMessage").ValueKind);

        var userInfo = await client.GetAsync("/api/users/me/info");
        Assert.Equal(HttpStatusCode.OK, userInfo.StatusCode);
        var userInfoJson = await userInfo.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ana", userInfoJson.GetProperty("firstName").GetString());
        Assert.Equal("Prueba", userInfoJson.GetProperty("lastName").GetString());
        Assert.Equal("Secret Garden", userInfoJson.GetProperty("organization").GetProperty("name").GetString());
        Assert.Equal("TENANT_OWNER", userInfoJson.GetProperty("role").GetProperty("code").GetString());

        var currentUser = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, currentUser.StatusCode);
        var currentPermissions = (await currentUser.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("permissions").EnumerateArray().Select(permission => permission.GetString()).ToArray();
        Assert.Contains("inventory.stock.read", currentPermissions);
        Assert.Contains("inventory.ingredients.manage", currentPermissions);
        Assert.Contains("inventory.movements.manage", currentPermissions);
        Assert.Contains("inventory.complements.manage", currentPermissions);
        Assert.Contains("tables.operate", currentPermissions);
        Assert.Contains("settings.organization.manage", currentPermissions);
        Assert.Contains("settings.roles.manage", currentPermissions);

        var organization = await client.GetAsync("/api/settings/organization");
        Assert.Equal(HttpStatusCode.OK, organization.StatusCode);
        Assert.True((await organization.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("canEditDocument").GetBoolean());
        var updateOrganization = await client.PutAsJsonAsync("/api/settings/organization", new
        {
            name = "Secret Garden Bogotá",
            responsibleName = "Ana Prueba",
            document = "900123456",
            contactName = "Administración",
            email = "contacto@secretgarden.test",
            address = "Calle 1 # 2-3",
            country = "Colombia",
            state = "Bogotá D.C.",
            city = "Bogotá",
            phone = "+57 300 000 0000",
            website = "https://secretgarden.test"
        });
        Assert.Equal(HttpStatusCode.OK, updateOrganization.StatusCode);
        Assert.Equal("Secret Garden Bogotá", (await updateOrganization.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString());

        var business = await client.PutAsJsonAsync("/api/settings/business", new
        {
            usesTables = true,
            deliveryEnabled = true,
            requiresOpenCashRegister = false,
            showVoluntaryTip = true,
            tipMessage = "Servicio Voluntario",
            suggestedTipPercentage = 12
        });
        Assert.Equal(HttpStatusCode.OK, business.StatusCode);
        Assert.Equal(12, (await business.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("suggestedTipPercentage").GetInt32());

        var defaultPayments = await client.GetAsync("/api/settings/payment-methods?includeInactive=true");
        Assert.Equal(HttpStatusCode.OK, defaultPayments.StatusCode);
        Assert.Equal(3, (await defaultPayments.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
        var newPayment = await client.PostAsJsonAsync("/api/settings/payment-methods", new { name = "Bono", isIncludedInCashOpening = false });
        Assert.Equal(HttpStatusCode.OK, newPayment.StatusCode);

        var newRole = await client.PostAsJsonAsync("/api/settings/access/roles", new
        {
            name = "Auditor",
            description = "Consulta configuración",
            permissions = new[] { "settings.organization.read" }
        });
        Assert.Equal(HttpStatusCode.OK, newRole.StatusCode);
        var auditorRoleId = (await newRole.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var invitedEmail = $"invited-{Guid.NewGuid():N}@saviaup.test";
        var invitation = await client.PostAsJsonAsync("/api/settings/access/users", new { email = invitedEmail, roleId = auditorRoleId });
        Assert.Equal(HttpStatusCode.OK, invitation.StatusCode);
        Assert.Equal("PENDING", (await invitation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        using (var invitedClient = factory.CreateClient())
        {
            var invitedRegistration = await invitedClient.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Usuario",
                lastName = "Invitado",
                email = invitedEmail,
                password
            });
            Assert.Equal(HttpStatusCode.OK, invitedRegistration.StatusCode);
        }
        var organizationUsers = await client.GetAsync("/api/settings/access/users");
        Assert.Equal(HttpStatusCode.OK, organizationUsers.StatusCode);
        Assert.Contains((await organizationUsers.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),
            item => item.GetProperty("email").GetString() == invitedEmail && item.GetProperty("status").GetString() == "ACTIVE");

        var createCategory = await client.PostAsJsonAsync("/api/categories", new
        {
            name = "  Bebidas   frías ",
            description = "Bebidas preparadas en barra",
            imageUrl = "https://cdn.saviaup.test/categories/drinks.webp",
            isInventoryTracked = true
        });
        Assert.Equal(HttpStatusCode.OK, createCategory.StatusCode);
        var categoryJson = await createCategory.Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = categoryJson.GetProperty("id").GetGuid();
        Assert.Equal("Bebidas frías", categoryJson.GetProperty("name").GetString());
        Assert.True(categoryJson.GetProperty("isInventoryTracked").GetBoolean());
        Assert.True(categoryJson.GetProperty("isActive").GetBoolean());

        var duplicateCategory = await client.PostAsJsonAsync("/api/categories", new
        {
            name = "bebidas frías",
            isInventoryTracked = false
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateCategory.StatusCode);
        var duplicateJson = await duplicateCategory.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "CATEGORY_NAME_ALREADY_EXISTS",
            duplicateJson.GetProperty("error").GetProperty("code").GetString());

        var activeCategories = await client.GetAsync("/api/categories");
        Assert.Equal(HttpStatusCode.OK, activeCategories.StatusCode);
        Assert.Single((await activeCategories.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

        var updateCategory = await client.PutAsJsonAsync($"/api/categories/{categoryId}", new
        {
            name = "Bebidas sin alcohol",
            description = (string?)null,
            imageUrl = (string?)null,
            isInventoryTracked = false
        });
        Assert.Equal(HttpStatusCode.OK, updateCategory.StatusCode);
        var updatedCategoryJson = await updateCategory.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bebidas sin alcohol", updatedCategoryJson.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, updatedCategoryJson.GetProperty("description").ValueKind);
        Assert.False(updatedCategoryJson.GetProperty("isInventoryTracked").GetBoolean());

        var disableCategory = await client.PatchAsJsonAsync(
            $"/api/categories/{categoryId}/status",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, disableCategory.StatusCode);
        Assert.False((await disableCategory.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("isActive").GetBoolean());

        var activeCategoriesAfterDisable = await client.GetAsync("/api/categories");
        Assert.Empty((await activeCategoriesAfterDisable.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

        var allCategories = await client.GetAsync("/api/categories?includeInactive=true");
        var inactiveCategory = Assert.Single((await allCategories.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Equal(categoryId, inactiveCategory.GetProperty("id").GetGuid());
        Assert.False(inactiveCategory.GetProperty("isActive").GetBoolean());

        var deleteCategory = await client.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCategory.StatusCode);
        var categoriesAfterDelete = await client.GetAsync("/api/categories?includeInactive=true");
        Assert.Empty((await categoriesAfterDelete.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

        var unitsResponse = await client.GetAsync("/api/inventory/complements/units?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, unitsResponse.StatusCode);
        var unitsJson = await unitsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, unitsJson.GetProperty("totalCount").GetInt32());
        var units = unitsJson.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(["gr", "kg", "und"], units.Select(unit => unit.GetProperty("code").GetString()).Order());
        var gramUnitId = units.Single(unit => unit.GetProperty("code").GetString() == "gr").GetProperty("id").GetGuid();

        var createInventoryCategory = await client.PostAsJsonAsync("/api/categories", new
        {
            name = "Materia prima",
            isInventoryTracked = true
        });
        Assert.Equal(HttpStatusCode.OK, createInventoryCategory.StatusCode);
        var inventoryCategoryId = (await createInventoryCategory.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var createIngredient = await client.PostAsJsonAsync("/api/inventory/ingredients", new
        {
            categoryId = inventoryCategoryId,
            measurementUnitId = gramUnitId,
            name = "Café molido",
            description = "Tueste medio",
            minimumStock = 10,
            initialStock = 5
        });
        Assert.Equal(HttpStatusCode.OK, createIngredient.StatusCode);
        var ingredientJson = await createIngredient.Content.ReadFromJsonAsync<JsonElement>();
        var ingredientId = ingredientJson.GetProperty("id").GetGuid();
        Assert.Equal(5m, ingredientJson.GetProperty("currentStock").GetDecimal());
        Assert.True(ingredientJson.GetProperty("isBelowMinimum").GetBoolean());

        var inventory = await client.GetAsync("/api/inventory?page=1&pageSize=10&belowMinimum=true");
        Assert.Equal(HttpStatusCode.OK, inventory.StatusCode);
        var inventoryJson = await inventory.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((1, 1), (inventoryJson.GetProperty("totalCount").GetInt32(), inventoryJson.GetProperty("totalPages").GetInt32()));
        Assert.Equal("ingredient", inventoryJson.GetProperty("items")[0].GetProperty("itemType").GetString());

        var purchase = await client.PostAsJsonAsync("/api/inventory/movements", new
        {
            ingredientId,
            direction = "increase",
            reason = "purchase",
            quantity = 10,
            note = "Compra semanal"
        });
        Assert.Equal(HttpStatusCode.OK, purchase.StatusCode);
        Assert.Equal(15m, (await purchase.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stockAfter").GetDecimal());

        var waste = await client.PostAsJsonAsync("/api/inventory/movements", new
        {
            ingredientId,
            direction = "decrease",
            reason = "waste",
            quantity = 3
        });
        Assert.Equal(HttpStatusCode.OK, waste.StatusCode);
        Assert.Equal(12m, (await waste.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stockAfter").GetDecimal());

        var insufficientStock = await client.PostAsJsonAsync("/api/inventory/movements", new
        {
            ingredientId,
            direction = "decrease",
            reason = "loss",
            quantity = 20
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, insufficientStock.StatusCode);
        Assert.Equal("INVENTORY_INSUFFICIENT_STOCK",
            (await insufficientStock.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetProperty("code").GetString());

        var movements = await client.GetAsync($"/api/inventory/movements?ingredientId={ingredientId}&page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, movements.StatusCode);
        var movementsJson = await movements.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((3, 2, 2), (
            movementsJson.GetProperty("totalCount").GetInt32(),
            movementsJson.GetProperty("items").GetArrayLength(),
            movementsJson.GetProperty("totalPages").GetInt32()));

        var ingredients = await client.GetAsync("/api/inventory/ingredients?page=1&pageSize=10&search=caf%C3%A9");
        Assert.Equal(HttpStatusCode.OK, ingredients.StatusCode);
        Assert.Equal(1, (await ingredients.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt32());

        var deleteUsedUnit = await client.DeleteAsync($"/api/inventory/complements/units/{gramUnitId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteUsedUnit.StatusCode);
        var deleteIngredient = await client.DeleteAsync($"/api/inventory/ingredients/{ingredientId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteIngredient.StatusCode);
        var deleteUsedCategory = await client.DeleteAsync($"/api/categories/{inventoryCategoryId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteUsedCategory.StatusCode);

        var disableIngredient = await client.PatchAsJsonAsync(
            $"/api/inventory/ingredients/{ingredientId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, disableIngredient.StatusCode);
        Assert.False((await disableIngredient.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isActive").GetBoolean());

        var createCustomUnit = await client.PostAsJsonAsync(
            "/api/inventory/complements/units", new { code = "ml", name = "mililitros" });
        Assert.Equal(HttpStatusCode.OK, createCustomUnit.StatusCode);
        var customUnitId = (await createCustomUnit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var updateCustomUnit = await client.PutAsJsonAsync(
            $"/api/inventory/complements/units/{customUnitId}", new { code = "lt", name = "litros" });
        Assert.Equal(HttpStatusCode.OK, updateCustomUnit.StatusCode);
        var deleteCustomUnit = await client.DeleteAsync($"/api/inventory/complements/units/{customUnitId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCustomUnit.StatusCode);

        var createProductCategory = await client.PostAsJsonAsync("/api/categories", new
        {
            name = "Platos preparados",
            isInventoryTracked = false
        });
        Assert.Equal(HttpStatusCode.OK, createProductCategory.StatusCode);
        var productCategoryId = (await createProductCategory.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var createProduct = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Hamburguesa clásica",
            categoryId = productCategoryId,
            salePrice = 25900,
            description = "Preparada al momento",
            imageUrl = "https://cdn.saviaup.test/products/burger.webp",
            preparationTimeMinutes = 15,
            isInventoryTracked = true
        });
        Assert.Equal(HttpStatusCode.OK, createProduct.StatusCode);
        var productJson = await createProduct.Content.ReadFromJsonAsync<JsonElement>();
        var productId = productJson.GetProperty("id").GetGuid();
        Assert.Equal("NORMAL", productJson.GetProperty("type").GetString());
        Assert.False(productJson.GetProperty("isInventoryTracked").GetBoolean());

        var productList = await client.GetAsync(
            $"/api/products?page=1&pageSize=10&search=hamburguesa&categoryId={productCategoryId}&type=NORMAL");
        Assert.Equal(HttpStatusCode.OK, productList.StatusCode);
        var productListJson = await productList.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, productListJson.GetProperty("totalCount").GetInt32());
        Assert.Equal(productId, productListJson.GetProperty("items")[0].GetProperty("id").GetGuid());

        var updateProduct = await client.PutAsJsonAsync($"/api/products/{productId}", new
        {
            type = "COMBO",
            name = "Combo hamburguesa",
            categoryId = inventoryCategoryId,
            salePrice = 34900,
            description = (string?)null,
            imageUrl = (string?)null,
            preparationTimeMinutes = 20,
            isInventoryTracked = true
        });
        Assert.Equal(HttpStatusCode.OK, updateProduct.StatusCode);
        var updatedProductJson = await updateProduct.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("COMBO", updatedProductJson.GetProperty("type").GetString());
        Assert.True(updatedProductJson.GetProperty("isInventoryTracked").GetBoolean());

        var disableCategoryInventory = await client.PutAsJsonAsync(
            $"/api/categories/{inventoryCategoryId}", new
            {
                name = "Materia prima",
                description = (string?)null,
                imageUrl = (string?)null,
                isInventoryTracked = false
            });
        Assert.Equal(HttpStatusCode.OK, disableCategoryInventory.StatusCode);
        var productAfterCategoryChange = await client.GetAsync(
            $"/api/products?page=1&pageSize=10&categoryId={inventoryCategoryId}");
        var productAfterCategoryChangeJson = await productAfterCategoryChange.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.False(productAfterCategoryChangeJson.GetProperty("items")[0]
            .GetProperty("isInventoryTracked").GetBoolean());

        var disableProduct = await client.PatchAsJsonAsync(
            $"/api/products/{productId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, disableProduct.StatusCode);
        Assert.False((await disableProduct.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("isActive").GetBoolean());

        var activeProducts = await client.GetAsync("/api/products?page=1&pageSize=10");
        Assert.Equal(0, (await activeProducts.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("totalCount").GetInt32());
        var inactiveProducts = await client.GetAsync("/api/products?page=1&pageSize=10&includeInactive=true");
        Assert.Equal(1, (await inactiveProducts.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("totalCount").GetInt32());

        var deleteProduct = await client.DeleteAsync($"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteProduct.StatusCode);
        var deletedProduct = await client.PatchAsJsonAsync(
            $"/api/products/{productId}/status", new { isActive = true });
        Assert.Equal(HttpStatusCode.NotFound, deletedProduct.StatusCode);

        var createArea = await client.PostAsJsonAsync("/api/table-areas", new
        {
            name = "  Salón   principal ",
            order = 1,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, createArea.StatusCode);
        var areaJson = await createArea.Content.ReadFromJsonAsync<JsonElement>();
        var areaId = areaJson.GetProperty("id").GetGuid();
        Assert.Equal("Salón principal", areaJson.GetProperty("name").GetString());

        var createTable = await client.PostAsJsonAsync("/api/tables", new
        {
            diningAreaId = areaId,
            name = "Mesa 01",
            capacity = 4,
            positionX = 120,
            positionY = 80,
            shape = "RECTANGLE_HORIZONTAL",
            isDelivery = false,
            isCashRegister = false,
            status = "AVAILABLE"
        });
        Assert.Equal(HttpStatusCode.OK, createTable.StatusCode);
        var tableJson = await createTable.Content.ReadFromJsonAsync<JsonElement>();
        var tableId = tableJson.GetProperty("id").GetGuid();
        Assert.Equal("RECTANGLE_HORIZONTAL", tableJson.GetProperty("shape").GetString());

        var initialTables = await client.GetAsync("/api/tables/operation");
        Assert.Equal(HttpStatusCode.OK, initialTables.StatusCode);
        var initialTablesJson = await initialTables.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, initialTablesJson.GetProperty("metrics").GetProperty("available").GetInt32());
        Assert.Equal(
            "RECTANGLE_HORIZONTAL",
            initialTablesJson.GetProperty("areas")[0].GetProperty("tables")[0].GetProperty("shape").GetString());
        Assert.False(initialTablesJson.GetProperty("cashRegister").GetProperty("isInteractionBlocked").GetBoolean());

        var occupyTable = await client.PatchAsJsonAsync(
            $"/api/tables/{tableId}/operation",
            new { status = "OCCUPIED", activeOrderTotal = 18500 });
        Assert.Equal(HttpStatusCode.OK, occupyTable.StatusCode);
        var occupiedTableJson = await occupyTable.Content.ReadFromJsonAsync<JsonElement>();
        var activeOrderId = occupiedTableJson.GetProperty("activeOrderId").GetGuid();
        Assert.Equal("OCCUPIED", occupiedTableJson.GetProperty("status").GetString());

        var updateOrder = await client.PatchAsJsonAsync(
            $"/api/tables/{tableId}/order",
            new { activeOrderId, total = 42000 });
        Assert.Equal(HttpStatusCode.OK, updateOrder.StatusCode);
        Assert.Equal(42000m, (await updateOrder.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("activeOrderTotal").GetDecimal());

        var closeTable = await client.PatchAsJsonAsync(
            $"/api/tables/{tableId}/operation",
            new { status = "AVAILABLE" });
        Assert.Equal(HttpStatusCode.OK, closeTable.StatusCode);
        var closedTableJson = await closeTable.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, closedTableJson.GetProperty("activeOrderId").ValueKind);
        Assert.Equal(0m, closedTableJson.GetProperty("activeOrderTotal").GetDecimal());

        var deleteTable = await client.DeleteAsync($"/api/tables/{tableId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteTable.StatusCode);
        var deleteArea = await client.DeleteAsync($"/api/table-areas/{areaId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteArea.StatusCode);

        var list = await client.GetAsync("/api/tenants");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Single((await list.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

        var select = await client.PostAsJsonAsync($"/api/tenants/{tenantId}/select", new { });
        Assert.Equal(HttpStatusCode.OK, select.StatusCode);
        var selectJson = await select.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(selectJson.GetProperty("tokens").GetProperty("accessToken").GetString());

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(loginJson.GetProperty("requiresTenantSelection").GetBoolean());
        Assert.Equal(tenantId, loginJson.GetProperty("activeTenant").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Protected_Endpoint_Without_Jwt_Returns_Uniform_401()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/tenants");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AUTH_UNAUTHENTICATED", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Public_Translations_Are_Available()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/i18n/en");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sign in", json.GetProperty("auth.login.title").GetString());
        Assert.Equal("Operations", json.GetProperty("navigation.sections.operation").GetString());
        Assert.Equal("Categories", json.GetProperty("modules.categories").GetString());
    }
}
