using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Plaza docente: cargo asignado a una materia para un período académico.
/// </summary>
public class TeachingPosition
{
    public int Id { get; set; }
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public PositionType PositionType { get; set; }
    public int MaxStudents { get; set; }
    public bool IsVacant { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public long? DeactivatedByUserId { get; set; }
    public string? DeactivationReason { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? CommissionId { get; set; }
    public Commission? Commission { get; set; }

    public long? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public User? DeactivatedByUser { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<TeacherAssignment> Assignments { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PositionType
{
    Titular = 0,
    Adjunct = 1,
    JTP = 2,        // Jefe de Trabajos Prácticos
    Assistant = 3   // Ayudante
}
