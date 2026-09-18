using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaviaUp.Backend.Infrastructure;
using SaviaUp.Backend.Core.Billing;
using SaviaUp.Backend.Core.Orders;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;
using SaviaUp.Backend.Infrastructure.Persistence.Repositories;
using SaviaUp.Backend.Infrastructure.Time;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class OrganizationTimeZoneTests(SaviaUpApiFactory factory) : IClassFixture<SaviaUpApiFactory>
{
    private sealed class TenantScope(Guid id) : ITenantContext
    {
        public Guid TenantId => id;
        public bool HasTenant => true;
    }
    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-09-19T03:30:00Z");
    }

    [Theory]
    [InlineData("America/Bogota", "2026-09-18")]
    [InlineData("America/New_York", "2026-03-08")]
    [InlineData("America/New_York", "2026-11-01")]
    public async Task OrdersAndReceipts_IncludeStartAndExcludeNextLocalMidnight(string zone, string date)
    {
        var id = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        await using var platform = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await using var app = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new TenantScope(id));
        platform.Tenants.Add(new Tenant { Id = id, Name = "Local calendar", TimeZoneId = zone });
        await platform.SaveChangesAsync();
        var zones = new IanaTimeZoneService();
        var organization = new OrganizationTimeZone(platform);
        var day = DateOnly.Parse(date);
        var (start, end) = zones.GetUtcRangeForLocalDate(day, zone);
        var instants = new[] { start.AddTicks(-1), start, end.AddTicks(-1), end };
        foreach (var instant in instants)
        {
            var order = new Order { Id = Guid.NewGuid(), TenantId = id, CreatedAt = instant, UpdatedAt = instant, PaidAt = instant, Status = "PAID", SubtotalAmount = 10, TotalAmount = 10 };
            app.Orders.Add(order);
            app.OrderReceipts.Add(new OrderReceipt { Id = Guid.NewGuid(), TenantId = id, OrderId = order.Id, Order = order, CreatedAt = instant, ReceiptType = "PAYMENT" });
        }
        app.Orders.Add(new Order { Id = Guid.NewGuid(), TenantId = otherId, CreatedAt = start, PaidAt = start, Status = "PAID" });
        await app.SaveChangesAsync();

        var orders = await new GetOrdersPageUseCase(new OrderRepository(app), organization, zones).ExecuteAsync(id, new OrderQueryRequest(FromLocalDate: day, ToLocalDate: day), default);
        Assert.Equal(2, orders.Value!.TotalCount);
        var receipts = await new GetBillingReceiptsUseCase(new BillingRepository(app), new Clock(), organization, zones).ExecuteAsync(id, new BillingReceiptQueryRequest(FromLocalDate: day, ToLocalDate: day), default);
        Assert.Equal(2, receipts.Value!.TotalCount);

        var stats = await new StatisticsRepository(app, organization, zones)
            .GetDashboardStatisticsAsync(id, "custom_range", day, day, false, start, default);
        Assert.Equal(2, stats.SalesTrend.Single(p => p.Date == date).OrdersCount);
        Assert.Equal(20m, stats.SalesTrend.Single(p => p.Date == date).Sales);
    }

    [Fact]
    public async Task OrganizationZone_IsValidatedPersistedAndExposedInUserContext()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "Zone", lastName = "Test", email = $"zone-{Guid.NewGuid():N}@saviaup.test", password = "Secure123!*" });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("session").GetProperty("accessToken").GetString());
        response = await client.PostAsJsonAsync("/api/tenants", new { name = "Time zone test" });
        response.EnsureSuccessStatusCode();
        json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("tokens").GetProperty("accessToken").GetString());
        var invalid = await client.PutAsJsonAsync("/api/settings/organization", new { name = "Time zone test", timeZoneId = "UTC-5" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var valid = await client.PutAsJsonAsync("/api/settings/organization", new { name = "Time zone test", timeZoneId = "America/New_York" });
        valid.EnsureSuccessStatusCode();
        var info = await client.GetFromJsonAsync<JsonElement>("/api/users/me/info");
        Assert.Equal("America/New_York", info.GetProperty("organization").GetProperty("timeZoneId").GetString());
        var wrongFormat = await client.GetAsync("/api/orders?fromDate=2026-09-18T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, wrongFormat.StatusCode);
        var missingRangeEnd = await client.GetAsync("/api/statistics?period=custom_range&fromDate=2026-09-18");
        Assert.Equal(HttpStatusCode.BadRequest, missingRangeEnd.StatusCode);
        var customRange = await client.GetAsync("/api/statistics?period=custom_range&fromDate=2026-09-18&toDate=2026-09-18");
        customRange.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateExpense_AcceptsBusinessDateWithoutExpenseDate()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Expense",
            lastName = "Test",
            email = $"expense-{Guid.NewGuid():N}@saviaup.test",
            password = "Secure123!*"
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", json.GetProperty("session").GetProperty("accessToken").GetString());

        response = await client.PostAsJsonAsync("/api/tenants", new { name = "Expense endpoint test" });
        response.EnsureSuccessStatusCode();
        json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", json.GetProperty("tokens").GetProperty("accessToken").GetString());

        response = await client.PostAsJsonAsync("/api/expenses", new
        {
            name = "tomates",
            description = (string?)null,
            amount = 20000,
            isCashOut = true,
            paymentMethod = "Efectivo",
            supplierId = (Guid?)null,
            expenseDate = (DateTimeOffset?)null,
            businessDate = "2026-09-18"
        });

        response.EnsureSuccessStatusCode();
        json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-09-18", json.GetProperty("businessDate").GetString());
        Assert.Equal(
            DateTimeOffset.Parse("2026-09-18T05:00:00Z"),
            json.GetProperty("expenseDate").GetDateTimeOffset());
    }

    [Fact]
    public async Task ExpenseBusinessDate_IsIndependentOfCreationAndShiftUsesCreationOnly()
    {
        var id = Guid.NewGuid();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new TenantScope(id));
        var zones = new IanaTimeZoneService();
        var day = new DateOnly(2026, 9, 18);
        var (start, end) = zones.GetUtcRangeForLocalDate(day, "America/Bogota");
        db.Expenses.AddRange(
            new Expense { Id = Guid.NewGuid(), TenantId = id, BusinessDate = day, ExpenseDate = start.AddDays(-5), CreatedAt = start.AddDays(2), Amount = 10 },
            new Expense { Id = Guid.NewGuid(), TenantId = id, BusinessDate = day.AddDays(-1), ExpenseDate = start.AddDays(-1), CreatedAt = start, Amount = 20 },
            new Expense { Id = Guid.NewGuid(), TenantId = id, ExpenseDate = end.AddTicks(-1), CreatedAt = end.AddTicks(-1), Amount = 30 },
            new Expense { Id = Guid.NewGuid(), TenantId = id, ExpenseDate = end, CreatedAt = end, Amount = 40 });
        await db.SaveChangesAsync();
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PlatformDatabase"] = "Host=localhost;Database=unused",
            ["ConnectionStrings:ApplicationDatabase"] = "Host=localhost;Database=unused"
        }).Build());
        services.AddSingleton(db);
        await using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IExpenseRepository>();
        var daily = await repository.GetPageAsync(id, start, end, null, null, "ACTIVE", null, null, 1, 100, default, FromBusinessDate: day, ToBusinessDate: day.AddDays(1));
        Assert.Equal(40, daily.Items.Sum(e => e.Amount));
        var shift = await repository.GetPageAsync(id, start, end, null, null, "ACTIVE", null, null, 1, 100, default, ByCreatedAt: true);
        Assert.Equal(50, shift.Items.Sum(e => e.Amount));
    }

    [Fact]
    public async Task ExpenseConsecutive_RecoversWhenStoredCounterIsBehindExistingExpenses()
    {
        var tenantId = Guid.NewGuid();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new TenantScope(tenantId));
        db.Expenses.Add(new Expense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConsecutiveNumber = 839,
            Name = "Existing expense"
        });
        db.OrganizationParameters.Add(new OrganizationParameter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = "expenses.nextConsecutive",
            Value = "2",
            ValueType = "integer"
        });
        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PlatformDatabase"] = "Host=localhost;Database=unused",
            ["ConnectionStrings:ApplicationDatabase"] = "Host=localhost;Database=unused"
        }).Build());
        services.AddSingleton(db);
        await using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IExpenseRepository>();

        var consecutive = await repository.GetNextConsecutiveAsync(tenantId, default);

        Assert.Equal(840, consecutive);
        Assert.Equal("841", db.OrganizationParameters.Single().Value);
    }

    [Fact]
    public void PostgreSqlMappings_PreserveInstantsAndCalendarDates()
    {
        using var app = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=localhost;Database=unused").Options, new TenantScope(Guid.NewGuid()));
        using var platform = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);
        foreach (var model in new[] { app.Model, platform.Model })
            foreach (var entity in model.GetEntityTypes())
                foreach (var property in entity.GetProperties().Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(DateTimeOffset)))
                    Assert.Equal("timestamp with time zone", property.GetColumnType());
        Assert.Equal("date", app.Model.FindEntityType(typeof(Expense))!.FindProperty(nameof(Expense.BusinessDate))!.GetColumnType());
        var zones = new IanaTimeZoneService();
        var (start, end) = zones.GetUtcRangeForLocalDate(new DateOnly(2026, 3, 8), "America/New_York");
        var sql = app.Orders.Where(o => o.PaidAt >= start && o.PaidAt < end).ToQueryString();
        Assert.Contains(">=", sql);
        Assert.Contains(" < ", sql);
        Assert.DoesNotContain("date_trunc", sql);
    }
}
