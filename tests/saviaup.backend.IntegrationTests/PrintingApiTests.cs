using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Repositories;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class PrintingApiTests(SaviaUpApiFactory factory) : IClassFixture<SaviaUpApiFactory>
{
    [Fact]
    public async Task AgentEndpoint_WithoutDeviceCredential_ReturnsUniformUnauthorizedError()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/printing/agent/jobs/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PRINT_AGENT_CREDENTIAL_INVALID", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task AgentCredential_CanOnlyRecoverAndUpdateItsOwnTenantJobs()
    {
        var tenantA = await CreateTenantAndPairAgentAsync("Printing A");
        var tenantB = await CreateTenantAndPairAgentAsync("Printing B");
        var jobA = Guid.NewGuid();
        var jobB = Guid.NewGuid();
        await SeedJobAsync(tenantA, jobA);
        await SeedJobAsync(tenantB, jobB);

        using var agentAClient = factory.CreateClient();
        agentAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantA.DeviceToken);
        var pending = await agentAClient.GetAsync("/api/printing/agent/jobs/pending");

        Assert.Equal(HttpStatusCode.OK, pending.StatusCode);
        var jobs = (await pending.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        var only = Assert.Single(jobs);
        Assert.Equal(jobA, only.GetProperty("id").GetGuid());
        Assert.Equal(tenantA.LocationId, only.GetProperty("locationId").GetGuid());

        var crossTenantUpdate = await agentAClient.PostAsJsonAsync(
            $"/api/printing/agent/jobs/{jobB}/status", new { status = "PRINTED", error = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, crossTenantUpdate.StatusCode);

        var ownUpdate = await agentAClient.PostAsJsonAsync(
            $"/api/printing/agent/jobs/{jobA}/status", new { status = "PROCESSING", error = (string?)null });
        Assert.Equal(HttpStatusCode.OK, ownUpdate.StatusCode);
        Assert.Equal("PROCESSING", (await ownUpdate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedB = await database.PrintJobs.IgnoreQueryFilters().SingleAsync(x => x.Id == jobB);
        Assert.Equal(PrintJobStatuses.Pending, persistedB.Status);
    }

    [Fact]
    public async Task Routing_PrefersProductRoute_AndFallsBackToCategory_ForOneAgentWithMultipleZones()
    {
        var tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"printing-routing-{Guid.NewGuid():N}")
            .Options;
        await using var database = new ApplicationDbContext(options, new FixedTenantContext(tenantId));
        var now = DateTimeOffset.UtcNow;
        var location = new Location { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Principal", NormalizedName = "PRINCIPAL", IsActive = true, CreatedAt = now, UpdatedAt = now };
        var agent = new PrintAgent { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, Name = "PC Cocina", DeviceIdentifier = "device-routing", Hostname = "HOST", OperatingSystem = "Windows", Version = "1", Status = PrintAgentStatuses.Online, Enabled = true, CreatedAt = now, UpdatedAt = now };
        var category = new Category { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Bebidas", NormalizedName = "BEBIDAS", IsActive = true, CreatedAt = now, UpdatedAt = now };
        var overriddenProduct = new Product { Id = Guid.NewGuid(), TenantId = tenantId, CategoryId = category.Id, Name = "Limonada", Type = ProductType.Normal, SalePrice = 10, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var fallbackProduct = new Product { Id = Guid.NewGuid(), TenantId = tenantId, CategoryId = category.Id, Name = "Agua", Type = ProductType.Normal, SalePrice = 5, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var kitchenPrinter = Printer("Cocina", tenantId, location.Id, agent.Id, now);
        var barPrinter = Printer("Barra", tenantId, location.Id, agent.Id, now);
        var kitchen = Zone("Cocina", tenantId, location.Id, agent.Id, now);
        var bar = Zone("Barra", tenantId, location.Id, agent.Id, now);
        database.AddRange(location, agent, category, overriddenProduct, fallbackProduct, kitchenPrinter, barPrinter, kitchen, bar);
        database.PrintingZonePrinters.AddRange(
            new PrintingZonePrinter { TenantId = tenantId, PrintingZoneId = kitchen.Id, PrinterId = kitchenPrinter.Id },
            new PrintingZonePrinter { TenantId = tenantId, PrintingZoneId = bar.Id, PrinterId = barPrinter.Id });
        database.CategoryPrintingRoutes.Add(new CategoryPrintingRoute { TenantId = tenantId, PrintingZoneId = kitchen.Id, CategoryId = category.Id });
        database.ProductPrintingRoutes.Add(new ProductPrintingRoute { TenantId = tenantId, PrintingZoneId = bar.Id, ProductId = overriddenProduct.Id });
        await database.SaveChangesAsync();

        var destinations = await new PrintingRepository(database).ResolveDestinationsAsync(
            tenantId, [overriddenProduct.Id, fallbackProduct.Id], CancellationToken.None);

        var overridden = Assert.Single(destinations[overriddenProduct.Id]);
        Assert.Equal(bar.Id, overridden.ZoneId);
        Assert.Equal([barPrinter.Id], overridden.PrinterIds);
        var fallback = Assert.Single(destinations[fallbackProduct.Id]);
        Assert.Equal(kitchen.Id, fallback.ZoneId);
        Assert.Equal([kitchenPrinter.Id], fallback.PrinterIds);
        Assert.Equal(agent.Id, overridden.AgentId);
        Assert.Equal(agent.Id, fallback.AgentId);
    }

    [Fact]
    public async Task DefaultPrinter_UsesTheFirstActiveConfiguration_AndPromotesTheNextOneWhenRemoved()
    {
        var tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"printing-default-{Guid.NewGuid():N}")
            .Options;
        await using var database = new ApplicationDbContext(options, new FixedTenantContext(tenantId));
        var now = DateTimeOffset.UtcNow;
        var location = new Location { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Principal", NormalizedName = "PRINCIPAL", IsActive = true, CreatedAt = now, UpdatedAt = now };
        var agent = new PrintAgent { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, Name = "PC Cocina", DeviceIdentifier = "device-default", Hostname = "HOST", OperatingSystem = "Windows", Version = "1", Status = PrintAgentStatuses.Online, Enabled = true, CreatedAt = now, UpdatedAt = now };
        var first = Printer("Cocina", tenantId, location.Id, agent.Id, now);
        var second = Printer("Barra", tenantId, location.Id, agent.Id, now.AddMinutes(1));
        database.AddRange(location, agent, first, second);
        await database.SaveChangesAsync();
        var repository = new PrintingRepository(database);

        var initial = await repository.GetDefaultDestinationAsync(tenantId, CancellationToken.None);
        Assert.Equal(first.Id, Assert.Single(initial!.PrinterIds));

        database.Remove(first);
        await database.SaveChangesAsync();
        var promoted = await repository.GetDefaultDestinationAsync(tenantId, CancellationToken.None);

        Assert.Equal(second.Id, Assert.Single(promoted!.PrinterIds));
    }

    [Fact]
    public async Task AgentReportsWindowsPrinters_AndAdministratorCanSelectFromAvailableList()
    {
        var tenant = await CreateTenantAndPairAgentAsync("Printer discovery");
        using var agentClient = factory.CreateClient();
        agentClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenant.DeviceToken);

        var sync = await agentClient.PostAsJsonAsync("/api/printing/agent/printers/sync", new
        {
            printers = new[]
            {
                new { name = "EPSON Kitchen", isDefault = true },
                new { name = "Microsoft Print to PDF", isDefault = false }
            }
        });

        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        using var administrator = factory.CreateClient();
        administrator.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenant.AccessToken);
        var available = await administrator.GetAsync($"/api/printing/agents/{tenant.AgentId}/available-printers");
        Assert.Equal(HttpStatusCode.OK, available.StatusCode);
        var printers = (await available.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        Assert.Equal(2, printers.Length);
        Assert.Contains(printers, x => x.GetProperty("name").GetString() == "EPSON Kitchen"
            && x.GetProperty("isDefault").GetBoolean()
            && !x.GetProperty("isConfigured").GetBoolean());

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await database.Printers.IgnoreQueryFilters().AnyAsync(x => x.PrintAgentId == tenant.AgentId));
    }

    private async Task<PairedTenant> CreateTenantAndPairAgentAsync(string tenantName)
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@printing.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Print",
            lastName = "Admin",
            email,
            password = "Secure123!*"
        });
        var initial = await register.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", initial.GetProperty("session").GetProperty("accessToken").GetString());
        var createTenant = await client.PostAsJsonAsync("/api/tenants", new { name = tenantName });
        var tenant = await createTenant.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = tenant.GetProperty("tenant").GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", tenant.GetProperty("tokens").GetProperty("accessToken").GetString());

        var pairingResponse = await client.PostAsJsonAsync(
            "/api/printing/agents/pairing-codes", new { locationId = (Guid?)null, agentName = $"PC {tenantName}" });
        Assert.Equal(HttpStatusCode.OK, pairingResponse.StatusCode);
        var pairing = await pairingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var code = pairing.GetProperty("code").GetString();
        var locationId = pairing.GetProperty("locationId").GetGuid();

        using var anonymous = factory.CreateClient();
        var pairResponse = await anonymous.PostAsJsonAsync("/api/printing/agent/pair", new
        {
            pairingCode = code,
            deviceIdentifier = $"device-{Guid.NewGuid():N}",
            hostname = $"HOST-{tenantName}",
            operatingSystem = "Windows 11",
            version = "1.0.0",
            localIpAddress = "192.168.1.10"
        });
        Assert.Equal(HttpStatusCode.OK, pairResponse.StatusCode);
        var paired = await pairResponse.Content.ReadFromJsonAsync<JsonElement>();
        return new PairedTenant(
            tenantId,
            locationId,
            paired.GetProperty("agentId").GetGuid(),
            paired.GetProperty("deviceToken").GetString()!,
            tenant.GetProperty("tokens").GetProperty("accessToken").GetString()!);
    }

    private async Task SeedJobAsync(PairedTenant tenant, Guid jobId)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var printer = new Printer
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            LocationId = tenant.LocationId,
            PrintAgentId = tenant.AgentId,
            Name = "Kitchen Printer",
            ConnectionType = PrinterConnectionTypes.WindowsSpooler,
            LocalPrinterName = "Test Printer",
            PaperWidth = 80,
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var zone = new PrintingZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            LocationId = tenant.LocationId,
            PrintAgentId = tenant.AgentId,
            Name = "Kitchen",
            NormalizedName = "KITCHEN",
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        database.Printers.Add(printer);
        database.PrintingZones.Add(zone);
        database.PrintJobs.Add(new PrintJob
        {
            Id = jobId,
            TenantId = tenant.TenantId,
            LocationId = tenant.LocationId,
            PrintAgentId = tenant.AgentId,
            PrinterId = printer.Id,
            PrintingZoneId = zone.Id,
            SourceType = "ORDER",
            SourceId = Guid.NewGuid(),
            DocumentType = "KITCHEN_ORDER",
            PayloadJson = "{\"documentType\":\"KitchenOrder\",\"orderNumber\":\"CMD-000001\",\"items\":[]}",
            Status = PrintJobStatuses.Pending,
            Attempts = 0,
            CreatedAt = now,
            QueuedAt = now,
            CreatedByUserId = Guid.NewGuid()
        });
        await database.SaveChangesAsync();
    }

    private static Printer Printer(string name, Guid tenantId, Guid locationId, Guid agentId, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            PrintAgentId = agentId,
            Name = name,
            ConnectionType = PrinterConnectionTypes.WindowsSpooler,
            LocalPrinterName = name,
            PaperWidth = 80,
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static PrintingZone Zone(string name, Guid tenantId, Guid locationId, Guid agentId, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = locationId,
            PrintAgentId = agentId,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    private sealed record PairedTenant(
        Guid TenantId,
        Guid LocationId,
        Guid AgentId,
        string DeviceToken,
        string AccessToken);

    private sealed class FixedTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public bool HasTenant => true;
    }
}
