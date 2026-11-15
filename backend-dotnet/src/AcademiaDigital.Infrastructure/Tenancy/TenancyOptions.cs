using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AcademiaDigital.Infrastructure.Tenancy;

/// <summary>
/// Configuración multitenant (sección <c>Tenancy</c>). Sin ninguna configuración el sistema se
/// comporta como antes: una sola institución (ITSC) sobre la base de <c>DefaultConnection</c>.
/// </summary>
public sealed class TenancyOptions
{
    public string DefaultSlug { get; set; } = "itsc";
    public string DefaultName { get; set; } = "Instituto Tecnológico Superior Córdoba";
    /// <summary>Hosts que se asignan a la institución por defecto al arrancar (solo agrega, nunca quita).</summary>
    public string[] DefaultDomains { get; set; } = ["localhost", "127.0.0.1"];
}

/// <summary>
/// Arma las cadenas de conexión: todas las instituciones comparten servidor y credenciales de
/// <c>DefaultConnection</c> y solo cambia el nombre de la base.
/// </summary>
public sealed class TenantConnectionStrings
{
    private readonly string _baseConnectionString;

    public TenantConnectionStrings(IConfiguration configuration)
    {
        _baseConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");
        DefaultDatabaseName = new NpgsqlConnectionStringBuilder(_baseConnectionString).Database
            ?? throw new InvalidOperationException("DefaultConnection debe indicar la base de datos.");
        ControlDatabaseName = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("ControlConnection") ?? For($"{DefaultDatabaseName}_control")).Database!;
        Control = configuration.GetConnectionString("ControlConnection") ?? For(ControlDatabaseName);
    }

    /// <summary>Base de la institución original; no se renombra para no migrar datos existentes.</summary>
    public string DefaultDatabaseName { get; }
    public string ControlDatabaseName { get; }
    public string Control { get; }

    public string For(string databaseName)
        => new NpgsqlConnectionStringBuilder(_baseConnectionString) { Database = databaseName }.ConnectionString;
}
