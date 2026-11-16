namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Pedido de una institución que quiere su plataforma (formulario "Pide tu plataforma" de la
/// landing). Vive en el esquema de control. El operador lo revisa en admin.dryvn.org y, si lo
/// aprueba, crea la institución (<see cref="TenantSlug"/>).
/// </summary>
public class PlatformRequest
{
    public Guid Id { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    /// <summary>Subdominio pedido (ej. "usb" para usb.dryvn.org). Opcional.</summary>
    public string? RequestedSlug { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Location { get; set; }
    public int? StudentsEstimate { get; set; }
    public string? Message { get; set; }
    public PlatformRequestStatus Status { get; set; } = PlatformRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    /// <summary>Institución creada a partir del pedido (al aprobarlo).</summary>
    public string? TenantSlug { get; set; }
}

public enum PlatformRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
