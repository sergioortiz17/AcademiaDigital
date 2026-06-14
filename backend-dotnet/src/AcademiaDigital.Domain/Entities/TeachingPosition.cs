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

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = [];
}

public enum PositionType
{
    Titular = 0,
    Adjunct = 1,
    JTP = 2,        // Jefe de Trabajos Prácticos
    Assistant = 3   // Ayudante
}
