using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Gender { get; set; }
    public string? Cuil { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? PhoneCode { get; set; }
    public string? Phone { get; set; }

    // Datos personales y de contacto que cada usuario carga desde su perfil, sea cual sea su rol.
    // Son los que piden las planillas del instituto (base de estudiantes y listado de contactos).
    /// <summary>Tipo de documento: DNI, LC, LE, CI o PAS. Por defecto DNI.</summary>
    public string? DocumentType { get; set; }
    /// <summary>Segundo teléfono (fijo o celular), texto libre.</summary>
    public string? AlternatePhone { get; set; }
    public string? AddressLine { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    /// <summary>Departamento de la provincia (por ejemplo, Capital, Río Segundo).</summary>
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    /// <summary>Obra social o prepaga (texto libre).</summary>
    public string? HealthInsurance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    public UserRole Role { get; set; } = UserRole.Alumno;
    /// <summary>Alta pendiente o aprobada. Un usuario pendiente no puede iniciar sesión.</summary>
    public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.Approved;
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}
