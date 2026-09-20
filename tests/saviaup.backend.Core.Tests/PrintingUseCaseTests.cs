using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using SaviaUp.Backend.Core.Printing;
using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.MultiTenancy;

namespace SaviaUp.Backend.Core.Tests;

public sealed class PrintingUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TenantContext_UsesAuthenticatedAgentTenant_WhenThereIsNoUserTenant()
    {
        var tenantId = Guid.NewGuid();
        var user = new Mock<ICurrentUserContext>();
        user.SetupGet(x => x.TenantId).Returns((Guid?)null);
        var agent = new Mock<IPrintAgentContext>();
        agent.SetupGet(x => x.TenantId).Returns(tenantId);

        var context = new TenantContext(user.Object, agent.Object);

        Assert.True(context.HasTenant);
        Assert.Equal(tenantId, context.TenantId);
    }

    [Fact]
    public async Task JobFactory_RoutesOneAgentToMultipleZones_AndCreatesOneJobPerPrinter()
    {
        var tenantId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var kitchenZoneId = Guid.NewGuid();
        var barZoneId = Guid.NewGuid();
        var kitchenPrinterId = Guid.NewGuid();
        var barPrinterId = Guid.NewGuid();
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.ResolveDestinationsAsync(
                tenantId, It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { productId })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<PrintingDestination>>
            {
                [productId] =
                [
                    new(locationId, agentId, kitchenZoneId, "Cocina", [kitchenPrinterId]),
                    new(locationId, agentId, barZoneId, "Barra", [barPrinterId])
                ]
            });
        List<PrintJob>? saved = null;
        repository.Setup(x => x.AddJobsAsync(It.IsAny<IEnumerable<PrintJob>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PrintJob>, CancellationToken>((jobs, _) => saved = jobs.ToList())
            .Returns(Task.CompletedTask);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = 42,
            CreatedByUserName = "Ana",
            Table = new RestaurantTable { Name = "Mesa 12" },
            Observations = "Alergia al maní"
        };
        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = productId,
            ProductName = "Hamburguesa",
            Quantity = 2,
            Notes = "Sin cebolla"
        };

        var settings = new Mock<ISettingsRepository>();
        settings.Setup(x => x.GetParametersAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(
            [new OrganizationParameter { Key = SettingsDefaults.EnableOrderPrintZones, Value = "true" }]);

        var notifications = await new PrintJobFactory(repository.Object, settings.Object).CreateForOrderItemsAsync(
            tenantId, order, [item], Guid.NewGuid(), Now, CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, job =>
        {
            Assert.Equal(tenantId, job.TenantId);
            Assert.Equal(agentId, job.PrintAgentId);
            Assert.Equal(PrintJobStatuses.Pending, job.Status);
            Assert.Equal(Now, job.CreatedAt);
        });
        Assert.Equal([kitchenZoneId, barZoneId], saved.Select(x => x.PrintingZoneId));
        Assert.Equal([kitchenPrinterId, barPrinterId], saved.Select(x => x.PrinterId));
        Assert.All(notifications, notification => Assert.Equal(agentId, notification.AgentId));
        using var payload = JsonDocument.Parse(saved[0].PayloadJson);
        Assert.Equal("CMD-000042", payload.RootElement.GetProperty("orderNumber").GetString());
        Assert.Equal("Mesa 12", payload.RootElement.GetProperty("table").GetString());
    }

    [Fact]
    public async Task JobFactory_DoesNotCreateJobs_WhenOrderPrintZonesAreDisabled()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IPrintingRepository>();
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(x => x.GetParametersAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(
            [new OrganizationParameter { Key = SettingsDefaults.EnableOrderPrintZones, Value = "false" }]);

        var notifications = await new PrintJobFactory(repository.Object, settings.Object).CreateForOrderItemsAsync(
            tenantId, new Order { Id = Guid.NewGuid(), TenantId = tenantId }, [], Guid.NewGuid(), Now, CancellationToken.None);

        Assert.Empty(notifications);
        repository.Verify(x => x.ResolveDestinationsAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddJobsAsync(It.IsAny<IEnumerable<PrintJob>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pairing_IsOneTime_AndOnlyPersistsHashedDeviceCredential()
    {
        var tenantId = Guid.NewGuid();
        var location = new Location { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Principal", IsActive = true };
        var pairing = new PrintAgentPairingCode
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = location.Id,
            Location = location,
            AgentName = "PC Caja",
            CodeHash = "hash:ABC123",
            CreatedAt = Now,
            ExpiresAt = Now.AddMinutes(10),
            CreatedByUserId = Guid.NewGuid()
        };
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetPairingCodeByHashAsync("hash:ABC123", It.IsAny<CancellationToken>())).ReturnsAsync(pairing);
        repository.Setup(x => x.TryConsumePairingCodeAsync(pairing.Id, Now, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetAgentByDeviceAsync(tenantId, location.Id, "device-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrintAgent?)null);
        repository.Setup(x => x.AddAgentAsync(It.IsAny<PrintAgent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        PrintAgentCredential? credential = null;
        repository.Setup(x => x.AddCredentialAsync(It.IsAny<PrintAgentCredential>(), It.IsAny<CancellationToken>()))
            .Callback<PrintAgentCredential, CancellationToken>((value, _) => credential = value)
            .Returns(Task.CompletedTask);
        var tokens = new Mock<ITokenGenerator>();
        tokens.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(value => $"hash:{value}");
        tokens.Setup(x => x.Generate()).Returns("plain-device-token");
        var unitOfWork = UnitOfWork();
        var useCase = new PrintAgentUseCase(repository.Object, tokens.Object, Clock(),
            Options.Create(new PrintingOptions()), unitOfWork.Object);
        var request = new PairPrintAgentRequest("abc123", "device-1", "CAJA-01", "Windows 11", "1.0.0", "192.168.1.20");

        var first = await useCase.PairAsync(request, CancellationToken.None);
        var second = await useCase.PairAsync(request, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal("plain-device-token", first.Value!.DeviceToken);
        Assert.NotNull(credential);
        Assert.Equal("hash:plain-device-token", credential.TokenHash);
        Assert.DoesNotContain("plain-device-token", pairing.CodeHash);
        Assert.Equal(Now, pairing.ConsumedAt);
        Assert.False(second.IsSuccess);
        repository.Verify(x => x.AddCredentialAsync(It.IsAny<PrintAgentCredential>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PrinterSync_StoresDiscoveries_WithoutCreatingConfiguredPrinters()
    {
        var tenantId = Guid.NewGuid();
        var agent = new PrintAgent { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = Guid.NewGuid(), Enabled = true };
        var stale = new PrintAgentDiscoveredPrinter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = agent.LocationId,
            PrintAgentId = agent.Id,
            Name = "Old printer",
            NormalizedName = "OLD PRINTER",
            IsAvailable = true,
            LastSeenAt = Now.AddDays(-1)
        };
        var discoveries = new List<PrintAgentDiscoveredPrinter> { stale };
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetAgentForUpdateAsync(tenantId, agent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        repository.Setup(x => x.GetDiscoveredPrintersAsync(tenantId, agent.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(discoveries);
        repository.Setup(x => x.GetPrintersAsync(tenantId, agent.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repository.Setup(x => x.AddDiscoveredPrinterAsync(It.IsAny<PrintAgentDiscoveredPrinter>(), It.IsAny<CancellationToken>()))
            .Callback<PrintAgentDiscoveredPrinter, CancellationToken>((printer, _) => discoveries.Add(printer))
            .Returns(Task.CompletedTask);
        var useCase = new PrintAgentUseCase(repository.Object, Mock.Of<ITokenGenerator>(), Clock(),
            Options.Create(new PrintingOptions()), UnitOfWork().Object);

        var result = await useCase.SyncPrintersAsync(tenantId, agent.Id,
            new SyncDiscoveredPrintersRequest([new DiscoveredPrinterDto("EPSON Kitchen", true)]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(stale.IsAvailable);
        var available = Assert.Single(result.Value!, x => x.IsAvailable);
        Assert.Equal("EPSON Kitchen", available.Name);
        Assert.True(available.IsDefault);
        Assert.False(available.IsConfigured);
        repository.Verify(x => x.AddPrinterAsync(It.IsAny<Printer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DiscoveryRequest_NotifiesOnlyTheSelectedAgent()
    {
        var tenantId = Guid.NewGuid();
        var agent = new PrintAgent { Id = Guid.NewGuid(), TenantId = tenantId, Enabled = true };
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetAgentAsync(tenantId, agent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        var realtime = new Mock<IPrintingRealtimeNotifier>();
        realtime.Setup(x => x.PrinterDiscoveryRequestedAsync(agent.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await Administration(repository, realtime)
            .RequestPrinterDiscoveryAsync(tenantId, agent.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        realtime.Verify(x => x.PrinterDiscoveryRequestedAsync(agent.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Retry_ReusesFailedJob_WithoutCreatingDuplicate()
    {
        var tenantId = Guid.NewGuid();
        var job = Job(tenantId, PrintJobStatuses.Failed);
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetJobAsync(tenantId, job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        var realtime = new Mock<IPrintingRealtimeNotifier>();
        realtime.Setup(x => x.JobAvailableAsync(job.PrintAgentId, job.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var useCase = Administration(repository, realtime);

        var result = await useCase.RetryJobAsync(tenantId, job.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PrintJobStatuses.Pending, job.Status);
        Assert.Null(job.LastError);
        Assert.Null(job.FailedAt);
        repository.Verify(x => x.AddJobsAsync(It.IsAny<IEnumerable<PrintJob>>(), It.IsAny<CancellationToken>()), Times.Never);
        realtime.Verify(x => x.JobAvailableAsync(job.PrintAgentId, job.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reprint_CreatesAuditedJob_AndPreservesOriginal()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var original = Job(tenantId, PrintJobStatuses.Printed);
        original.PayloadJson = JsonSerializer.Serialize(new KitchenOrderPrintPayload(
            "KitchenOrder", "CMD-000123", "Mesa 4", "Ana", Now, [], null, false));
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetJobAsync(tenantId, original.Id, It.IsAny<CancellationToken>())).ReturnsAsync(original);
        PrintJob? created = null;
        repository.Setup(x => x.AddJobsAsync(It.IsAny<IEnumerable<PrintJob>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PrintJob>, CancellationToken>((jobs, _) => created = jobs.Single())
            .Returns(Task.CompletedTask);
        var realtime = new Mock<IPrintingRealtimeNotifier>();
        realtime.Setup(x => x.JobAvailableAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var useCase = Administration(repository, realtime);

        var result = await useCase.ReprintJobAsync(tenantId, original.Id, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(created);
        Assert.NotEqual(original.Id, created.Id);
        Assert.Equal(original.Id, created.OriginalPrintJobId);
        Assert.True(created.IsReprint);
        Assert.Equal(userId, created.ReprintRequestedByUserId);
        Assert.Equal(Now, created.ReprintRequestedAt);
        Assert.Equal(PrintJobStatuses.Printed, original.Status);
        Assert.Contains("\"isReprint\":true", created.PayloadJson);
        realtime.Verify(x => x.JobAvailableAsync(created.PrintAgentId, created.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AgentCannotUpdateJobAssignedToAnotherAgent()
    {
        var tenantId = Guid.NewGuid();
        var assignedAgentId = Guid.NewGuid();
        var callerAgentId = Guid.NewGuid();
        var job = Job(tenantId, PrintJobStatuses.Pending);
        job.PrintAgentId = assignedAgentId;
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetJobAsync(tenantId, job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        var unitOfWork = UnitOfWork();
        var useCase = new PrintAgentUseCase(repository.Object, Mock.Of<ITokenGenerator>(), Clock(),
            Options.Create(new PrintingOptions()), unitOfWork.Object);

        var result = await useCase.UpdateJobStatusAsync(
            tenantId, callerAgentId, job.Id, new PrintJobStatusRequest(PrintJobStatuses.Printed, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PrintJobStatuses.Pending, job.Status);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PrintingAdministrationUseCase Administration(
        Mock<IPrintingRepository> repository,
        Mock<IPrintingRealtimeNotifier> realtime)
        => new(repository.Object, Mock.Of<ICategoryRepository>(), Mock.Of<IProductRepository>(),
            Mock.Of<ITokenGenerator>(), Clock(), realtime.Object, Mock.Of<IUnpairedPrintAgentRegistry>(), Mock.Of<IOrganizationTimeZone>(),
            Mock.Of<ITimeZoneService>(), Options.Create(new PrintingOptions()), UnitOfWork().Object);

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        unit.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<Result<PairPrintAgentResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<PairPrintAgentResponse>>> action, CancellationToken token) => action(token));
        return unit;
    }

    private static IDateTimeProvider Clock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(Now);
        return clock.Object;
    }

    private static PrintJob Job(Guid tenantId, string status)
    {
        var location = new Location { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Principal" };
        var agent = new PrintAgent { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, Name = "PC Caja" };
        var printer = new Printer { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, PrintAgentId = agent.Id, Name = "Cocina", PaperWidth = 80 };
        var zone = new PrintingZone { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, PrintAgentId = agent.Id, Name = "Cocina" };
        return new PrintJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = location.Id,
            PrintAgentId = agent.Id,
            PrinterId = printer.Id,
            PrintingZoneId = zone.Id,
            SourceType = "ORDER",
            SourceId = Guid.NewGuid(),
            DocumentType = "KITCHEN_ORDER",
            PayloadJson = "{}",
            Status = status,
            Attempts = 2,
            CreatedAt = Now.AddMinutes(-2),
            QueuedAt = Now.AddMinutes(-2),
            FailedAt = Now.AddMinutes(-1),
            LastError = "Paper out",
            CreatedByUserId = Guid.NewGuid(),
            Location = location,
            PrintAgent = agent,
            Printer = printer,
            PrintingZone = zone
        };
    }
}
