using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

public class Student
{
    public long Id { get; set; }
    public string LegajoNumber { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public StudentStatus Status { get; set; } = StudentStatus.Regular;
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    /// <summary>Obra social o prepaga (texto libre).</summary>
    public string? HealthInsurance { get; set; }
    /// <summary>Fecha de egreso (la carga Administración al marcarlo egresado).</summary>
    public DateOnly? GraduatedOn { get; set; }
    /// <summary>Libro matriz y folio donde está registrado el alumno (para el analítico).</summary>
    public string? RegistryBook { get; set; }
    public string? RegistryFolio { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public ICollection<StudentCareer> Careers { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudentStatus
{
    Regular = 0,
    Libre = 1,
    Graduated = 2,
    Withdrawn = 3
}
