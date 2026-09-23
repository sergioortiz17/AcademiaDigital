namespace AcademiaDigital.Domain.Entities;

public class StudentCareer
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Año calendario de ingreso a esta carrera = la "cohorte de ingreso" real (estable en el tiempo,
    /// a diferencia de la Division que cambia año a año al avanzar). Se captura desde el alta para
    /// reportes futuros (ej. "% de graduados de la cohorte 2024"); todavía no se usa operativamente.
    /// </summary>
    public int AdmissionYear { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StudentStudyPlan> StudyPlans { get; set; } = [];
    public ICollection<StudentAcademicAssignment> AcademicAssignments { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<StudentRematriculation> Rematriculations { get; set; } = [];
}
