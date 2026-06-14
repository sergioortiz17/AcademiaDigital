namespace AcademiaDigital.Domain.Entities;

public class StudentStudyPlan
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int StudyPlanId { get; set; }
    public StudyPlan StudyPlan { get; set; } = null!;
    public bool IsCurrent { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string? MigrationReason { get; set; }
}
