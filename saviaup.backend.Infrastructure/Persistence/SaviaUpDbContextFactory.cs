using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SaviaUp.Backend.Infrastructure.Persistence;

public sealed class SaviaUpDbContextFactory : IDesignTimeDbContextFactory<SaviaUpDbContext>
{
    public SaviaUpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SaviaUp")
            ?? "Host=localhost;Port=5432;Database=saviaup;Username=postgres;Password=postgres";
        var builder = new DbContextOptionsBuilder<SaviaUpDbContext>();
        builder.UseNpgsql(connectionString);
        return new SaviaUpDbContext(builder.Options);
    }
}
