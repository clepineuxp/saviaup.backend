using Moq;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Expenses;
using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using Xunit;

namespace SaviaUp.Backend.Core.Tests;

public sealed class ExpenseUseCaseTests
{
    [Fact]
    public async Task CreateExpense_WithValidData_ReturnsSuccessAndConsecutive()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new Mock<IExpenseRepository>();
        var supplierRepository = new Mock<ISupplierRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Expense? persisted = null;

        repository.Setup(x => x.GetNextConsecutiveAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        repository.Setup(x => x.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()))
            .Callback<Expense, CancellationToken>((e, _) => persisted = e)
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<Result<ExpenseDto>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<Result<ExpenseDto>>>, CancellationToken>(
                (action, token) => action(token));

        var useCase = new CreateExpenseUseCase(
            repository.Object,
            supplierRepository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object, new FixedOrganizationTimeZone(), TestSupport.TimeZones());

        var request = new CreateExpenseRequest(
            "Compra insumos de aseo",
            "Detergente y servilletas",
            120500.50m,
            true,
            "Efectivo",
            null,
            TestSupport.Now);

        var result = await useCase.ExecuteAsync(tenantId, userId, "Test User", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(15, persisted.ConsecutiveNumber);
        Assert.Equal("Compra insumos de aseo", persisted.Name);
        Assert.Equal(120500.50m, persisted.Amount);
        Assert.True(persisted.IsCashOut);
        Assert.Equal("ACTIVE", persisted.Status);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateExpense_WithBusinessDate_UsesOrganizationStartOfDay()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<IExpenseRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Expense? persisted = null;

        repository.Setup(x => x.GetNextConsecutiveAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(840);
        repository.Setup(x => x.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()))
            .Callback<Expense, CancellationToken>((expense, _) => persisted = expense)
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<Result<ExpenseDto>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<Result<ExpenseDto>>>, CancellationToken>(
                (action, token) => action(token));

        var useCase = new CreateExpenseUseCase(
            repository.Object,
            Mock.Of<ISupplierRepository>(),
            new FixedClock(TestSupport.Now),
            unitOfWork.Object,
            new FixedOrganizationTimeZone(),
            TestSupport.TimeZones());

        var request = new CreateExpenseRequest(
            "tomates", null, 20000m, true, "Efectivo", null, null, new DateOnly(2026, 9, 18));

        var result = await useCase.ExecuteAsync(
            tenantId, Guid.NewGuid(), "Test User", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(840, persisted.ConsecutiveNumber);
        Assert.Equal(new DateOnly(2026, 9, 18), persisted.BusinessDate);
        Assert.Equal(new DateTimeOffset(2026, 9, 18, 5, 0, 0, TimeSpan.Zero), persisted.ExpenseDate);
    }

    [Fact]
    public async Task UpdateExpense_ChangesEditableDataAndPreservesImmutableFinancialData()
    {
        var tenantId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var originalDate = new DateTimeOffset(2026, 9, 18, 5, 0, 0, TimeSpan.Zero);
        var originalBusinessDate = new DateOnly(2026, 9, 18);
        var repository = new Mock<IExpenseRepository>();
        var settingsRepository = new Mock<ISettingsRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var existing = new Expense
        {
            Id = expenseId,
            TenantId = tenantId,
            ConsecutiveNumber = 16,
            Name = "Compra inicial",
            NormalizedName = "COMPRA INICIAL",
            Amount = 98000m,
            IsCashOut = true,
            PaymentMethod = "Efectivo",
            ExpenseDate = originalDate,
            BusinessDate = originalBusinessDate,
            Status = "ACTIVE",
            CreatedAt = TestSupport.Now,
            UpdatedAt = TestSupport.Now
        };

        repository.Setup(x => x.GetByIdAsync(tenantId, expenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        settingsRepository.Setup(x => x.GetParameterValueAsync(
                tenantId,
                SettingsDefaults.LockExpenseFinancialFieldsAfterCreation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var useCase = new UpdateExpenseUseCase(
            repository.Object,
            Mock.Of<ISupplierRepository>(),
            settingsRepository.Object,
            new FixedClock(TestSupport.Now.AddHours(1)),
            unitOfWork.Object,
            new FixedOrganizationTimeZone(),
            TestSupport.TimeZones());

        var result = await useCase.ExecuteAsync(
            tenantId,
            expenseId,
            Guid.NewGuid(),
            "Admin User",
            new UpdateExpenseRequest(
                "Compra corregida",
                "Detalle actualizado",
                "Tarjeta",
                null,
                Amount: 1m,
                IsCashOut: false,
                BusinessDate: new DateOnly(2026, 9, 20)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Compra corregida", existing.Name);
        Assert.Equal("Detalle actualizado", existing.Description);
        Assert.Equal("Tarjeta", existing.PaymentMethod);
        Assert.Equal(98000m, existing.Amount);
        Assert.True(existing.IsCashOut);
        Assert.Equal(originalDate, existing.ExpenseDate);
        Assert.Equal(originalBusinessDate, existing.BusinessDate);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateExpense_WhenFinancialFieldsAreUnlocked_ChangesAmountDateAndCashOrigin()
    {
        var tenantId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var repository = new Mock<IExpenseRepository>();
        var settingsRepository = new Mock<ISettingsRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var existing = new Expense
        {
            Id = expenseId,
            TenantId = tenantId,
            ConsecutiveNumber = 17,
            Name = "Compra inicial",
            NormalizedName = "COMPRA INICIAL",
            Amount = 98_000m,
            IsCashOut = true,
            PaymentMethod = "Efectivo",
            ExpenseDate = new DateTimeOffset(2026, 9, 18, 5, 0, 0, TimeSpan.Zero),
            BusinessDate = new DateOnly(2026, 9, 18),
            Status = "ACTIVE",
            CreatedAt = TestSupport.Now,
            UpdatedAt = TestSupport.Now
        };

        repository.Setup(x => x.GetByIdAsync(tenantId, expenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        settingsRepository.Setup(x => x.GetParameterValueAsync(
                tenantId,
                SettingsDefaults.LockExpenseFinancialFieldsAfterCreation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("false");

        var useCase = new UpdateExpenseUseCase(
            repository.Object,
            Mock.Of<ISupplierRepository>(),
            settingsRepository.Object,
            new FixedClock(TestSupport.Now.AddHours(1)),
            unitOfWork.Object,
            new FixedOrganizationTimeZone(),
            TestSupport.TimeZones());

        var result = await useCase.ExecuteAsync(
            tenantId,
            expenseId,
            Guid.NewGuid(),
            "Admin User",
            new UpdateExpenseRequest(
                "Compra corregida",
                null,
                "Transferencia",
                null,
                Amount: 120_000m,
                IsCashOut: false,
                BusinessDate: new DateOnly(2026, 9, 20)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(120_000m, existing.Amount);
        Assert.False(existing.IsCashOut);
        Assert.Equal(new DateOnly(2026, 9, 20), existing.BusinessDate);
        Assert.Equal(new DateTimeOffset(2026, 9, 20, 5, 0, 0, TimeSpan.Zero), existing.ExpenseDate);
    }

    [Fact]
    public async Task AnnulExpense_WhenActive_ReturnsAnnulledState()
    {
        var tenantId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var repository = new Mock<IExpenseRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var existing = new Expense
        {
            Id = expenseId,
            TenantId = tenantId,
            ConsecutiveNumber = 10,
            Name = "Compra café",
            Amount = 55000m,
            Status = "ACTIVE",
            CreatedAt = TestSupport.Now
        };

        repository.Setup(x => x.GetByIdAsync(tenantId, expenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var useCase = new AnnulExpenseUseCase(
            repository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var request = new AnnulExpenseRequest("Error en valor digitado");

        var result = await useCase.ExecuteAsync(tenantId, expenseId, Guid.NewGuid(), "Admin User", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ANNULLED", existing.Status);
        Assert.Equal("Error en valor digitado", existing.AnnulledReason);
        Assert.Equal(TestSupport.Now, existing.AnnulledAt);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
