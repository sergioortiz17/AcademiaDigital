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
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled;
    public decimal? FinalGrade { get; set; }

    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public long StudentCareerId { get; set; }
    public StudentCareer StudentCareer { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? StudyPlanCourseId { get; set; }
    public StudyPlanCourse? StudyPlanCourse { get; set; }

    public int? TeachingPositionId { get; set; }
    public TeachingPosition? TeachingPosition { get; set; }

    public int? EnrollmentPeriodId { get; set; }
    public EnrollmentPeriod? EnrollmentPeriod { get; set; }

    public string? Shift { get; set; }
}

public enum EnrollmentStatus
{
    Enrolled = 0,
    Regularized = 1,
    Approved = 2,
    Promoted = 3,
    Failed = 4,
    Withdrawn = 5
}
