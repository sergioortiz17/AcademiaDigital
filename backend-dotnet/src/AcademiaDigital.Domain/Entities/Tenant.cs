namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Institución (tenant) que usa la plataforma. Vive en el esquema de control. Todas las
/// instituciones comparten la base; cada una tiene su propio esquema (<see cref="SchemaName"/>).
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    /// <summary>Identificador corto y estable (ej. "itsc", "ucv"). Inmutable una vez creado.</summary>
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Esquema de PostgreSQL con las tablas académicas de la institución ("public" para ITSC).</summary>
    public string SchemaName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Dominio de correo del personal docente (ej. "itscordoba.edu.ar"). Quien se registra con un
    /// email de este dominio queda como Profesor (pendiente de alta). Null: sin esta regla.
    /// </summary>
    public string? TeacherEmailDomain { get; set; }

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
