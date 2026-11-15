using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Tenancy;

/// <summary>Crea un <see cref="AppDbContext"/> fuera del request (arranque, alta de institución).</summary>
public sealed class TenantDbContextFactory(TenantConnectionStrings connectionStrings)
{
    public AppDbContext Create(string databaseName)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionStrings.For(databaseName)).Options);
}
