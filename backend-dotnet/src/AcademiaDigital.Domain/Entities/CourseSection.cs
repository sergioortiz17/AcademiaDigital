using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Comisión de una materia: la sección real donde el profesor dicta y carga notas/asistencia.
/// Se asocia a una materia (CourseId), a una División/grupo de cursada (DivisionId), a un docente,
/// y a un cuatrimestre-año (AcademicYear + Semester, o IsAnnual=true para materias anuales).
/// </summary>
public class CourseSection
{
    public int Id { get; set; }
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    /// <summary>Materia anual: se dicta todo el año, no aplica un cuatrimestre puntual.</summary>
    public bool IsAnnual { get; set; } = false;
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

    public int? DivisionId { get; set; }
    public Division? Division { get; set; }

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
