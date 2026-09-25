using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Repositories;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class CategoryRepositoryTests
{
    [Fact]
    public void UsageCountsQuery_WithCategoryFilter_CanBeTranslatedByNpgsql()
    {
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
                .Options,
            new TenantScope(tenantId));
        var repository = new CategoryRepository(context);

        var sql = repository
            .UsageCountsQuery(tenantId, categoryId)
            .ToQueryString();

        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(categoryId.ToString(), sql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TenantScope(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public bool HasTenant => true;
    }
}
