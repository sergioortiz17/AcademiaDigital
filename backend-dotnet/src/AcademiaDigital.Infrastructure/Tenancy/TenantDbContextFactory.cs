using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Tenancy;

/// <summary>Crea un <see cref="AppDbContext"/> fuera del request (arranque, alta de institución).</summary>
public sealed class TenantDbContextFactory(TenantConnectionStrings connectionStrings)
{
    /// <param name="schemaName">Esquema de la institución en la base compartida.</param>
    public AppDbContext Create(string schemaName)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSchemaNpgsql(connectionStrings.For(schemaName)).Options);
}
