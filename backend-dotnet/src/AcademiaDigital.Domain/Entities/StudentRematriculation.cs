namespace AcademiaDigital.Domain.Entities;

public sealed class StudentRematriculation
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public long StudentCareerId { get; set; }
    public StudentCareer StudentCareer { get; set; } = null!;
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public int StudyPlanId { get; set; }
    public StudyPlan StudyPlan { get; set; } = null!;
    public int CommissionId { get; set; }
    public Commission Commission { get; set; } = null!;
    public int AcademicYear { get; set; }
    public int YearNumber { get; set; }
    public DateTime RematriculatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public string? Notes { get; set; }
}
