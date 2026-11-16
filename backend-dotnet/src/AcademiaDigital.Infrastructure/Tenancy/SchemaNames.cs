using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademiaDigital.Infrastructure.Tenancy;

/// <summary>
/// Esquemas de PostgreSQL de la base compartida (uno por institución). Los nombres se validan
/// con una lista blanca estricta antes de usarse en SQL, porque un identificador no se puede
/// pasar como parámetro.
/// </summary>
public static partial class SchemaNames
{
    [GeneratedRegex("^[a-z][a-z0-9_]{0,62}$")]
    private static partial Regex Pattern();

    public static string Validate(string schemaName)
    {
        if (string.IsNullOrEmpty(schemaName) || !Pattern().IsMatch(schemaName)
            || schemaName.StartsWith("pg_", StringComparison.Ordinal))
            throw new ArgumentException($"'{schemaName}' no es un nombre de esquema válido.");
        return schemaName;
    }

    /// <summary>
    /// Crea la base (si todavía no existe, p. ej. un PostgreSQL local recién creado) y el esquema
    /// del contexto, para poder migrarlo. Idempotente.
    /// </summary>
    public static async Task EnsureCreatedAsync(DbContext db, string schemaName, CancellationToken ct = default)
    {
        var schema = Validate(schemaName);
        var creator = db.Database.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync(ct))
            await creator.CreateAsync(ct);

#pragma warning disable EF1002 // identificador validado por lista blanca (no admite comillas ni espacios)
        await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schema}\"", ct);
#pragma warning restore EF1002
    }
}

public static class SchemaDbContextOptions
{
    /// <summary>
    /// PostgreSQL apuntando a un esquema de la base compartida. La tabla de historial de
    /// migraciones se ubica explícitamente en ese mismo esquema, para que cada institución (y el
    /// catálogo) lleve su propio historial y nunca lea el de otro esquema.
    /// </summary>
    public static DbContextOptionsBuilder UseSchemaNpgsql(this DbContextOptionsBuilder options, string connectionString)
        => options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TenantConnectionStrings.SchemaOf(connectionString)));

    public static DbContextOptionsBuilder<T> UseSchemaNpgsql<T>(this DbContextOptionsBuilder<T> options, string connectionString)
        where T : DbContext
        => (DbContextOptionsBuilder<T>)((DbContextOptionsBuilder)options).UseSchemaNpgsql(connectionString);
}
