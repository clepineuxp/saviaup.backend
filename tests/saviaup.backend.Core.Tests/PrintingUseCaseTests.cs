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
        repository.Setup(x => x.GetDefaultDestinationAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PrintingDestination?)null);

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
    public async Task JobFactory_UsesTheDefaultPrinter_WhenNoProductOrCategoryRouteExists()
    {
        var tenantId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var printerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.ResolveDestinationsAsync(tenantId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<PrintingDestination>>());
        repository.Setup(x => x.GetDefaultDestinationAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrintingDestination(locationId, agentId, null, "Impresora predeterminada: Cocina", [printerId]));
        List<PrintJob>? saved = null;
        repository.Setup(x => x.AddJobsAsync(It.IsAny<IEnumerable<PrintJob>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PrintJob>, CancellationToken>((jobs, _) => saved = jobs.ToList())
            .Returns(Task.CompletedTask);
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(x => x.GetParametersAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(
            [new OrganizationParameter { Key = SettingsDefaults.EnableOrderPrintZones, Value = "true" }]);

        await new PrintJobFactory(repository.Object, settings.Object).CreateForOrderItemsAsync(
            tenantId,
            new Order { Id = Guid.NewGuid(), TenantId = tenantId, OrderNumber = 9, CreatedByUserName = "Ana" },
            [
                new OrderItem { Id = Guid.NewGuid(), ProductId = productId, ProductName = "Hamburguesa", Quantity = 1 },
                new OrderItem { Id = Guid.NewGuid(), ProductName = "Venta libre", Quantity = 1 }
            ],
            Guid.NewGuid(), Now, CancellationToken.None);

        var job = Assert.Single(saved!);
        Assert.Equal(printerId, job.PrinterId);
        Assert.Null(job.PrintingZoneId);
        using var payload = JsonDocument.Parse(job.PayloadJson);
        Assert.Equal(2, payload.RootElement.GetProperty("items").GetArrayLength());
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
        repository.Setup(x => x.GetAgentByDeviceAsync(tenantId, "device-1", It.IsAny<CancellationToken>()))
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

    [Theory]
    [InlineData(PrintJobStatuses.Pending)]
    [InlineData(PrintJobStatuses.Processing)]
    [InlineData(PrintJobStatuses.Failed)]
    public async Task CancelJob_CancelsEveryUnfinishedJob(string status)
    {
        var tenantId = Guid.NewGuid();
        var job = Job(tenantId, status);
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetJobAsync(tenantId, job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        var realtime = new Mock<IPrintingRealtimeNotifier>();
        realtime.Setup(x => x.JobCancelledAsync(job.PrintAgentId, job.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await Administration(repository, realtime).CancelJobAsync(tenantId, job.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PrintJobStatuses.Cancelled, job.Status);
        Assert.Null(job.FailedAt);
        Assert.Null(job.LastError);
        realtime.Verify(x => x.JobCancelledAsync(job.PrintAgentId, job.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PrintJobStatuses.Printed)]
    [InlineData(PrintJobStatuses.Cancelled)]
    public async Task CancelJob_RejectsTerminalJobs(string status)
    {
        var tenantId = Guid.NewGuid();
        var job = Job(tenantId, status);
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetJobAsync(tenantId, job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        var realtime = new Mock<IPrintingRealtimeNotifier>();

        var result = await Administration(repository, realtime).CancelJobAsync(tenantId, job.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(status, job.Status);
        realtime.Verify(x => x.JobCancelledAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TestPrint_CreatesDirectQueueJobWithoutZone()
    {
        var tenantId = Guid.NewGuid();
        var location = new Location { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Principal" };
        var agent = new PrintAgent { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, Location = location, Name = "PC Caja", Enabled = true };
        var printer = new Printer { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, PrintAgentId = agent.Id, Name = "Cocina Epson", Enabled = true, Location = location, PrintAgent = agent };
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetAgentAsync(tenantId, agent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        repository.Setup(x => x.GetPrinterAsync(tenantId, printer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(printer);
        PrintJob? created = null;
        repository.Setup(x => x.AddJobsAsync(It.IsAny<IEnumerable<PrintJob>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PrintJob>, CancellationToken>((jobs, _) => created = jobs.Single())
            .Returns(Task.CompletedTask);
        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Restaurante Savia" });
        var realtime = new Mock<IPrintingRealtimeNotifier>();
        realtime.Setup(x => x.JobAvailableAsync(agent.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await Administration(repository, realtime, tenantRepository.Object)
            .CreateTestJobAsync(tenantId, agent.Id, printer.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(created);
        Assert.Equal("TEST", created.SourceType);
        Assert.Equal("TEST_PRINT", created.DocumentType);
        Assert.Null(created.PrintingZoneId);
        using var payload = JsonDocument.Parse(created.PayloadJson);
        Assert.Equal("Cocina Epson", payload.RootElement.GetProperty("printerName").GetString());
        Assert.Equal("Restaurante Savia", payload.RootElement.GetProperty("organizationName").GetString());
        Assert.Equal("Prueba de impresión de Savia Up", payload.RootElement.GetProperty("footerMessage").GetString());
        realtime.Verify(x => x.JobAvailableAsync(agent.Id, created.Id, It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task CreatePrinter_ReactivatesTheDisabledPrinterWithTheSameWindowsQueue()
    {
        var tenantId = Guid.NewGuid();
        var agent = new PrintAgent { Id = Guid.NewGuid(), TenantId = tenantId, LocationId = Guid.NewGuid(), Enabled = true };
        var disabledPrinter = new Printer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = agent.LocationId,
            PrintAgentId = agent.Id,
            Name = "Cocina anterior",
            ConnectionType = PrinterConnectionTypes.WindowsSpooler,
            LocalPrinterName = "EPSON Cocina",
            PaperWidth = 80,
            Enabled = false,
            CreatedAt = Now.AddDays(-1)
        };
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetAgentAsync(tenantId, agent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        repository.Setup(x => x.GetDisabledPrintersForUpdateAsync(tenantId, agent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([disabledPrinter]);

        var result = await Administration(repository, new Mock<IPrintingRealtimeNotifier>()).CreatePrinterAsync(
            tenantId,
            new SavePrinterRequest(agent.Id, "Cocina", PrinterConnectionTypes.WindowsSpooler, "EPSON Cocina", null, null, 80),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(disabledPrinter.Id, result.Value!.Id);
        Assert.True(disabledPrinter.Enabled);
        Assert.Equal("Cocina", disabledPrinter.Name);
        repository.Verify(x => x.AddPrinterAsync(It.IsAny<Printer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListDiscoveredAgents_OnlyReturnsAgentsThatCanBeLinkedForTheCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var registry = new Mock<IUnpairedPrintAgentRegistry>();
        var disabledDiscovery = new DiscoveredPrintAgentDto(Guid.NewGuid(), "disabled-device", "PC Cocina", "Windows", "1", null, Now);
        var activeDiscovery = new DiscoveredPrintAgentDto(Guid.NewGuid(), "active-device", "PC Barra", "Windows", "1", null, Now);
        registry.Setup(x => x.ListAsync("203.0.113.10", It.IsAny<CancellationToken>()))
            .ReturnsAsync([disabledDiscovery, activeDiscovery]);
        var repository = new Mock<IPrintingRepository>();
        repository.Setup(x => x.GetAgentsAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new PrintAgent { TenantId = tenantId, DeviceIdentifier = "disabled-device", Enabled = false },
            new PrintAgent { TenantId = tenantId, DeviceIdentifier = "active-device", Enabled = true }
        ]);

        var result = await Administration(repository, new Mock<IPrintingRealtimeNotifier>(), discoveryRegistry: registry.Object)
            .ListDiscoveredAgentsAsync(tenantId, "203.0.113.10", CancellationToken.None);

        var agent = Assert.Single(result.Value!);
        Assert.Equal("disabled-device", agent.DeviceIdentifier);
        Assert.True(agent.IsReactivation);
    }

    private static PrintingAdministrationUseCase Administration(
        Mock<IPrintingRepository> repository,
        Mock<IPrintingRealtimeNotifier> realtime,
        ITenantRepository? tenantRepository = null,
        IUnpairedPrintAgentRegistry? discoveryRegistry = null)
        => new(repository.Object, Mock.Of<ICategoryRepository>(), Mock.Of<IProductRepository>(),
            tenantRepository ?? Mock.Of<ITenantRepository>(), Mock.Of<ITokenGenerator>(), Clock(), realtime.Object, discoveryRegistry ?? Mock.Of<IUnpairedPrintAgentRegistry>(), Mock.Of<IOrganizationTimeZone>(),
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
