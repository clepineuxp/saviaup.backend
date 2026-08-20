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
    }
}
