using System.Text.RegularExpressions;

namespace AcademiaDigital.Domain.Services;

/// <summary>
/// Regla "email institucional → docente": quien se registra con un email del dominio docente de
/// su institución queda como Profesor (pendiente de alta). Compara el dominio exacto, sin
/// subdominios: "a@itscordoba.edu.ar" coincide con "itscordoba.edu.ar", "a@x.itscordoba.edu.ar" no.
/// </summary>
public static partial class TeacherEmailDomains
{
    [GeneratedRegex(@"^(?=.{1,253}$)([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$")]
    private static partial Regex DomainPattern();

    /// <summary>Minúsculas, sin "@" inicial ni espacios. Null si está vacío.</summary>
    /// <exception cref="ArgumentException">Si no es un dominio válido.</exception>
    public static string? Normalize(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return null;
        var normalized = domain.Trim().TrimStart('@').ToLowerInvariant();
        if (!DomainPattern().IsMatch(normalized))
            throw new ArgumentException($"'{domain}' no es un dominio de correo válido (ej. itscordoba.edu.ar).");
        return normalized;
    }

    public static bool Matches(string email, string? teacherDomain)
    {
        if (string.IsNullOrWhiteSpace(teacherDomain)) return false;
        var at = email.Trim().LastIndexOf('@');
        return at > 0 && string.Equals(email.Trim()[(at + 1)..], teacherDomain, StringComparison.OrdinalIgnoreCase);
    }
}
