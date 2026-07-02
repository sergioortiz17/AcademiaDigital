namespace AcademiaDigital.Domain.Entities;

public class StudyPlanCourse
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan StudyPlan { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int YearNumber { get; set; }
    public int Semester { get; set; }
    public int? CourseTypeId { get; set; }
    public CourseType? CourseType { get; set; }
    public int SortOrder { get; set; }
    public bool IsMandatory { get; set; } = true;
    public decimal? Credits { get; set; }
    public int? WorkloadHours { get; set; }
    public bool IsAnnual { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public CourseApprovalRule? ApprovalRule { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}
