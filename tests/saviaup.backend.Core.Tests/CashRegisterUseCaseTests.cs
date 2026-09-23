using Moq;
using SaviaUp.Backend.Core.CashRegisters;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;
using Xunit;

namespace SaviaUp.Backend.Core.Tests;

public sealed class CashRegisterUseCaseTests
{
    [Fact]
    public void CashRegisterShiftDto_CalculatesInitialAndTotalInCash()
    {
        var shift = new CashRegisterShiftDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Caja principal",
            "CLOSED",
            Guid.NewGuid(),
            "Apertura",
            TestSupport.Now,
            Guid.NewGuid(),
            "Cierre",
            TestSupport.Now.AddHours(8),
            TotalSalesAmount: 450_000m,
            TotalTipsAmount: 16_000m,
            TotalCollectedAmount: 466_000m,
            TotalExpensesAmount: 66_000m,
            OpeningBalancesJson: """[{"methodName":"Efectivo","amount":100000},{"methodName":"Tarjeta","amount":25000}]""",
            ClosingSummaryJson: null);

        Assert.Equal(125_000m, shift.InitialOpeningAmount);
        Assert.Equal(525_000m, shift.TotalInCashAmount);
    }

    [Fact]
    public void CashRegisterShiftSummaryDto_CalculatesInitialAndTotalInCash()
    {
        var summary = new CashRegisterShiftSummaryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Caja principal",
            "CLOSED",
            "Apertura",
            TestSupport.Now,
            TotalSalesAmount: 450_000m,
            TotalTipsAmount: 16_000m,
            TotalCollectedAmount: 466_000m,
            TotalExpensesAmount: 66_000m,
            MethodSummaries:
            [
                new PaymentMethodClosingSummaryDto(
                    "Efectivo", 100_000m, 350_000m, 16_000m, 66_000m, 366_000m, 400_000m, 400_000m, 0),
                new PaymentMethodClosingSummaryDto(
                    "Tarjeta", 25_000m, 100_000m, 0, 0, 100_000m, 125_000m, 125_000m, 0)
            ]);

        Assert.Equal(125_000m, summary.InitialOpeningAmount);
        Assert.Equal(525_000m, summary.TotalInCashAmount);
    }

    [Fact]
    public async Task CreateCashRegister_NormalizesNameAndRejectsDuplicate()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ICashRegisterRepository>();
        repository.Setup(repo => repo.NameExistsAsync(tenantId, "CAJA PRINCIPAL", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateCashRegisterUseCase(repository.Object, Mock.Of<ICashRegisterShiftRepository>(), Mock.Of<IUnitOfWork>(), new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(tenantId, new CreateCashRegisterRequest("  caja Principal  ", "Mostrador"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CashRegisterNameAlreadyExists, result.Error!.Code);
    }

    [Fact]
    public async Task CreateCashRegister_WhenActiveAlreadyExists_RejectsMultipleActive()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ICashRegisterRepository>();
        repository.Setup(repo => repo.NameExistsAsync(tenantId, "CAJA SECUNDARIA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(repo => repo.HasOtherActiveAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateCashRegisterUseCase(repository.Object, Mock.Of<ICashRegisterShiftRepository>(), Mock.Of<IUnitOfWork>(), new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(tenantId, new CreateCashRegisterRequest("Caja Secundaria", null, IsActive: true), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CashRegisterSingleActiveExceeded, result.Error!.Code);
    }

    [Fact]
    public async Task CreateCashRegister_Success()
    {
        var tenantId = Guid.NewGuid();
        CashRegister? created = null;
        var repository = new Mock<ICashRegisterRepository>();
        repository.Setup(repo => repo.NameExistsAsync(tenantId, "CAJA 01", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(repo => repo.HasOtherActiveAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(repo => repo.AddAsync(It.IsAny<CashRegister>(), It.IsAny<CancellationToken>()))
            .Callback<CashRegister, CancellationToken>((cr, _) => created = cr)
            .Returns(Task.CompletedTask);

        var useCase = new CreateCashRegisterUseCase(repository.Object, Mock.Of<ICashRegisterShiftRepository>(), Mock.Of<IUnitOfWork>(), new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(tenantId, new CreateCashRegisterRequest("Caja 01", "Zona A", IsActive: true), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(created);
        Assert.Equal("Caja 01", created.Name);
        Assert.Equal("CAJA 01", created.NormalizedName);
        Assert.Equal("Zona A", created.Location);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task UpdateCashRegister_WhenActivatingSecondRegister_RejectsWithSingleActiveExceeded()
    {
        var tenantId = Guid.NewGuid();
        var registerId = Guid.NewGuid();
        var existing = new CashRegister
        {
            Id = registerId,
            TenantId = tenantId,
            Name = "Caja 02",
            NormalizedName = "CAJA 02",
            IsActive = false
        };

        var repository = new Mock<ICashRegisterRepository>();
        repository.Setup(repo => repo.GetByIdAsync(tenantId, registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository.Setup(repo => repo.NameExistsAsync(tenantId, "CAJA 02", registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(repo => repo.HasOtherActiveAsync(tenantId, registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new UpdateCashRegisterUseCase(repository.Object, Mock.Of<ICashRegisterShiftRepository>(), Mock.Of<IUnitOfWork>(), new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(tenantId, registerId, new UpdateCashRegisterRequest("Caja 02", null, IsActive: true), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CashRegisterSingleActiveExceeded, result.Error!.Code);
    }

    [Fact]
    public async Task SetStatus_TogglesActiveAndValidatesSingleActive()
    {
        var tenantId = Guid.NewGuid();
        var registerId = Guid.NewGuid();
        var existing = new CashRegister
        {
            Id = registerId,
            TenantId = tenantId,
            Name = "Caja 01",
            NormalizedName = "CAJA 01",
            IsActive = true
        };

        var repository = new Mock<ICashRegisterRepository>();
        repository.Setup(repo => repo.GetByIdAsync(tenantId, registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var useCase = new SetCashRegisterStatusUseCase(repository.Object, Mock.Of<ICashRegisterShiftRepository>(), Mock.Of<IUnitOfWork>(), new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(tenantId, registerId, new SetCashRegisterStatusRequest(IsActive: false), default);

        Assert.True(result.IsSuccess);
        Assert.False(existing.IsActive);
    }
}
