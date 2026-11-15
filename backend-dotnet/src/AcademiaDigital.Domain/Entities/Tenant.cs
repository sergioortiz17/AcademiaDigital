namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Institución (tenant) que usa la plataforma. Vive en la base de control, NO en la base
/// académica: cada institución tiene su propia base de datos (<see cref="DatabaseName"/>).
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    /// <summary>Identificador corto y estable (ej. "itsc", "ucv"). Inmutable una vez creado.</summary>
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TenantDomain> Domains { get; set; } = [];
}

/// <summary>Host (sin esquema ni puerto) desde el que se accede a una institución.</summary>
public class TenantDomain
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Host { get; set; } = string.Empty;
}
