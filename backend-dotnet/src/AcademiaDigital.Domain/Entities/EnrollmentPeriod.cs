namespace AcademiaDigital.Domain.Entities;

public class EnrollmentPeriod
{
    public int Id { get; set; }
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public int StudyPlanId { get; set; }
    public StudyPlan StudyPlan { get; set; } = null!;
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public int QuotasMorning { get; set; }
    public int QuotasAfternoon { get; set; }
    public int QuotasEvening { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public ICollection<Enrollment> Enrollments { get; set; } = [];
}
