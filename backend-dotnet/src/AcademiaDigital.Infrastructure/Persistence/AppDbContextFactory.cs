using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AcademiaDigital.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used exclusively by the EF Core tools (<c>dotnet ef</c>) to build an
/// <see cref="AppDbContext"/> without spinning up the API host. It is NOT used at runtime —
/// the running application always resolves the context from DI (see <c>AddInfrastructure</c>).
///
/// Having this factory keeps <c>dotnet ef migrations add</c> working in environments where
/// building the WebApplication host is not possible (e.g. a constrained CI/sandbox with a low
/// inotify limit). Scaffolding a migration only inspects the model to emit schema DDL; the
/// connection is never opened, so the connection string only needs to be a syntactically valid
/// Npgsql string. For real design-time DB operations (e.g. <c>database update</c>) provide the
/// actual connection via the <c>DOTNET_EF_CONNECTION</c> environment variable.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DOTNET_EF_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=AcademiaDigital;Username=postgres;Password=postgres;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
