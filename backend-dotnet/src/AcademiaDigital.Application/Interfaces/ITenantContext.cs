namespace AcademiaDigital.Application.Interfaces;

/// <summary>Institución resuelta para el request actual.</summary>
/// <param name="IsDefault">Institución original (ITSC): conserva el comportamiento previo al multitenant.</param>
/// <param name="TeacherEmailDomain">Dominio de correo docente; null si la institución no usa la regla.</param>
public sealed record TenantInfo(Guid Id, string Slug, string Name, string SchemaName, bool IsDefault,
    string? TeacherEmailDomain = null);

public interface ITenantContext
{
    /// <summary>Institución del request. Lanza si el request no pasó por la resolución de tenant.</summary>
    TenantInfo Current { get; }
}
