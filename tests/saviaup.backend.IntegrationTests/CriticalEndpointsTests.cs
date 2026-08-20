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
        Assert.Equal(9, sections.Sum(section => section.GetProperty("modules").GetArrayLength()));
        Assert.All(sections, section => Assert.Empty(section.GetProperty("options").EnumerateArray()));
        Assert.Equal([1, 2, 3, 4], sections.Select(section => section.GetProperty("order").GetInt32()));
        Assert.False(sections[0].GetProperty("isGrouped").GetBoolean());
        Assert.True(sections[1].GetProperty("isGrouped").GetBoolean());
        Assert.Equal(
            ["orders", "reports", "billing"],
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
