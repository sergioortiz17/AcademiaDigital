using System.Text.RegularExpressions;

namespace AcademiaDigital.Domain.Services;

/// <summary>
/// Reglas puras para dar de alta una institución: formato del slug, de los hosts y nombre del
/// esquema derivado. Todas las instituciones comparten la base; cada una tiene su esquema.
/// Todo lo que termina en un identificador SQL o en un header se valida acá con una lista
/// blanca estricta.
/// </summary>
public sealed partial class TenantPolicy
{
    public const int MinPasswordLength = 8;

    // Se reservan los esquemas que ya existen en la base compartida ("public" = ITSC,
    // "control" = catálogo de instituciones, "finance" = microservicio Finance) y los
    // subdominios propios del sitio (admin.dryvn.org, academiadigital.dryvn.org, ...).
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.Ordinal)
    {
        "public", "control", "finance", "platform", "admin", "api", "www", "academiadigital",
        "postgres", "template0", "template1"
    };

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{0,28}[a-z0-9])?$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)*$")]
    private static partial Regex HostPattern();

    public string NormalizeSlug(string? slug)
    {
        var normalized = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (!SlugPattern().IsMatch(normalized))
            throw new ArgumentException(
                "El identificador de la institución debe tener entre 1 y 30 caracteres: letras minúsculas, números y guiones (no al inicio ni al final).");
        // "pg-x" daría el esquema "pg_x": PostgreSQL reserva el prefijo pg_.
        if (ReservedSlugs.Contains(normalized) || normalized.StartsWith("pg-", StringComparison.Ordinal)
            || normalized.StartsWith("finance-", StringComparison.Ordinal))
            throw new ArgumentException($"El identificador '{normalized}' está reservado.");
        return normalized;
    }

    public string NormalizeName(string? name)
    {
        var normalized = (name ?? string.Empty).Trim();
        if (normalized.Length is < 3 or > 200)
            throw new ArgumentException("El nombre de la institución debe tener entre 3 y 200 caracteres.");
        return normalized;
    }

    /// <summary>
    /// Acepta "ucv.edu.ar", "https://ucv.edu.ar:4200/algo" o "UCV.localhost" y devuelve el host
    /// en minúsculas, sin esquema, puerto ni ruta.
    /// </summary>
    public string NormalizeHost(string? host)
    {
        var value = (host ?? string.Empty).Trim().ToLowerInvariant();

        var schemeIndex = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex >= 0) value = value[(schemeIndex + 3)..];
        var pathIndex = value.IndexOfAny(['/', '?', '#']);
        if (pathIndex >= 0) value = value[..pathIndex];
        var portIndex = value.LastIndexOf(':');
        if (portIndex >= 0) value = value[..portIndex];

        if (value.Length is 0 or > 253 || !HostPattern().IsMatch(value))
            throw new ArgumentException($"El dominio '{host}' no es válido.");
        return value;
    }

    public IReadOnlyList<string> NormalizeHosts(IEnumerable<string>? hosts)
    {
        var normalized = (hosts ?? [])
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(NormalizeHost)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length == 0)
            throw new ArgumentException("La institución necesita al menos un dominio.");
        if (normalized.Length > 10)
            throw new ArgumentException("Una institución admite hasta 10 dominios.");
        return normalized;
    }

    /// <summary>Esquema de la institución en la base compartida: el slug con "_" en lugar de "-".</summary>
    public string SchemaNameFor(string normalizedSlug)
        => normalizedSlug.Replace('-', '_');

    public void ValidateAdmin(string? email, string? name, string? lastName, string? dni, string? password)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 254 || !IsEmail(email.Trim()))
            throw new ArgumentException("El email del administrador no es válido.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255)
            throw new ArgumentException("El nombre del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 255)
            throw new ArgumentException("El apellido del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(dni) || dni.Trim().Length > 20)
            throw new ArgumentException("El documento del administrador es obligatorio (máximo 20 caracteres).");
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength || password.Length > 128)
            throw new ArgumentException($"La contraseña del administrador debe tener entre {MinPasswordLength} y 128 caracteres.");
    }

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at == value.LastIndexOf('@') && at < value.Length - 3
            && value.IndexOf('.', at) > at + 1 && !value.Contains(' ');
    }
}
