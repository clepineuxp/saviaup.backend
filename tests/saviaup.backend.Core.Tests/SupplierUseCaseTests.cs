using Moq;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Suppliers;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using Xunit;

namespace SaviaUp.Backend.Core.Tests;

public sealed class SupplierUseCaseTests
{
    [Fact]
    public async Task CreateSupplier_WithValidData_ReturnsSuccess()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new Mock<ISupplierRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Supplier? persisted = null;

        repository.Setup(x => x.NameExistsAsync(tenantId, "DISTRIBUIDORA ALFA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repository.Setup(x => x.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Callback<Supplier, CancellationToken>((s, _) => persisted = s)
            .Returns(Task.CompletedTask);

        var useCase = new CreateSupplierUseCase(
            repository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var request = new CreateSupplierRequest(
            "Distribuidora Alfa",
            "Alfa Comercial",
            "900123456-1",
            "contacto@alfa.test",
            "3001234567",
            "Calle 10 #20-30");

        var result = await useCase.ExecuteAsync(tenantId, userId, "Test User", request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal("Distribuidora Alfa", persisted.Name);
        Assert.Equal("DISTRIBUIDORA ALFA", persisted.NormalizedName);
        Assert.Equal("900123456-1", persisted.Document);
        Assert.True(persisted.IsActive);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSupplier_WithDuplicateName_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ISupplierRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        repository.Setup(x => x.NameExistsAsync(tenantId, "PROVEEDOR DUP", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateSupplierUseCase(
            repository.Object,
            new FixedClock(TestSupport.Now),
            unitOfWork.Object);

        var request = new CreateSupplierRequest("Proveedor Dup", null, null, null, null, null);

        var result = await useCase.ExecuteAsync(tenantId, Guid.NewGuid(), "User", request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("SUPPLIER_NAME_ALREADY_EXISTS", result.Error.Code);
    }
}
