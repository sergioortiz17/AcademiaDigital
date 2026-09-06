using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AcademiaDigital.Finance.Infrastructure.Persistence;

// Used by `dotnet ef` at design time so the tool can build the context without spinning up
// the whole host. The connection string is irrelevant for migration scaffolding (no DB is
// contacted), but a valid Npgsql string keeps the provider happy.
public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=AcademiaDigital;Username=postgres;Password=postgres;SearchPath=finance")
            .Options;
        return new FinanceDbContext(options);
    }
}
