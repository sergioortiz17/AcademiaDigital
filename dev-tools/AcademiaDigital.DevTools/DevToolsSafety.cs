using Npgsql;

namespace AcademiaDigital.DevTools;

/// <summary>
/// Salvaguarda anti-producción. Esta herramienta ejecuta operaciones destructivas
/// (reset total de la base) y NUNCA debe poder apuntar a un ambiente real.
///
/// Doble condición, ambas obligatorias para arrancar:
///   1) ALLOW_DESTRUCTIVE_DB_TOOLS=true en el entorno.
///   2) El Host de la connection string es un host de desarrollo conocido
///      (localhost / 127.0.0.1 / db — el nombre del servicio Postgres en docker-compose).
/// Si alguna falla, la app lanza una excepción al inicio y no levanta.
/// </summary>
public static class DevToolsSafety
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "db" // service name del contenedor Postgres en docker-compose
    };

    public static void EnsureSafeOrThrow(string? connectionString)
    {
        var allowFlag = Environment.GetEnvironmentVariable("ALLOW_DESTRUCTIVE_DB_TOOLS");
        if (!string.Equals(allowFlag, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "DevTools bloqueado: falta ALLOW_DESTRUCTIVE_DB_TOOLS=true. " +
                "Esta herramienta es solo para desarrollo/testing y no debe correr en producción.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DevTools bloqueado: no hay ConnectionStrings:DefaultConnection configurada.");
        }

        string host;
        try
        {
            host = new NpgsqlConnectionStringBuilder(connectionString).Host ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "DevTools bloqueado: la connection string no es una cadena Npgsql válida.", ex);
        }

        if (!AllowedHosts.Contains(host))
        {
            throw new InvalidOperationException(
                $"DevTools bloqueado: el Host de la base ('{host}') no es un host de desarrollo conocido " +
                $"({string.Join(", ", AllowedHosts)}). Se rechaza para no operar contra un ambiente real.");
        }
    }

    /// <summary>Host de la DB, para mostrarlo en la UI como confirmación visual.</summary>
    public static string DescribeTarget(string? connectionString)
    {
        try
        {
            var b = new NpgsqlConnectionStringBuilder(connectionString);
            return $"{b.Host}:{b.Port}/{b.Database}";
        }
        catch
        {
            return "(desconocido)";
        }
    }
}
