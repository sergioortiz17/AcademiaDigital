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
    /// <summary>
    /// Dominio de correo docente de la institución por defecto. Se aplica al arrancar solo si la
    /// institución todavía no tiene uno (no pisa un valor cargado desde la plataforma).
    /// </summary>
    public string? DefaultTeacherEmailDomain { get; set; }
}

/// <summary>
/// Arma las cadenas de conexión de la base compartida. Todas las instituciones usan el mismo
/// servidor, la misma base y las mismas credenciales de <c>DefaultConnection</c>; solo cambia el
/// esquema (<c>Search Path</c>). El catálogo de instituciones está en el esquema <c>control</c>.
/// </summary>
public sealed class TenantConnectionStrings
{
    /// <summary>Esquema de la institución por defecto (ITSC): sus tablas ya estaban en public.</summary>
    public const string DefaultSchemaName = "public";
    public const string ControlSchemaName = "control";

    private readonly string _baseConnectionString;

    public TenantConnectionStrings(IConfiguration configuration)
    {
        _baseConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");
        if (string.IsNullOrWhiteSpace(new NpgsqlConnectionStringBuilder(_baseConnectionString).Database))
            throw new InvalidOperationException("DefaultConnection debe indicar la base de datos.");
        Control = configuration.GetConnectionString("ControlConnection") ?? For(ControlSchemaName);
    }

    public string Control { get; }

    /// <summary>Cadena de conexión de la base compartida con el esquema de la institución.</summary>
    public string For(string schemaName)
        => new NpgsqlConnectionStringBuilder(_baseConnectionString) { SearchPath = SchemaNames.Validate(schemaName) }.ConnectionString;

    /// <summary>Esquema al que apunta una cadena de conexión (su primer Search Path).</summary>
    public static string SchemaOf(string connectionString)
        => (new NpgsqlConnectionStringBuilder(connectionString).SearchPath ?? DefaultSchemaName).Split(',')[0].Trim();
}
