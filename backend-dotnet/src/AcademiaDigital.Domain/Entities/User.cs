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
