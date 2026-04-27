namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Inscripción de un alumno a una materia en un período académico determinado.
/// </summary>
public class Enrollment
{
    public long Id { get; set; }
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public decimal? FinalGrade { get; set; }

    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int? TeachingPositionId { get; set; }
    public TeachingPosition? TeachingPosition { get; set; }
}

public enum EnrollmentStatus
{
    Active = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3,
    Pending = 4
}
