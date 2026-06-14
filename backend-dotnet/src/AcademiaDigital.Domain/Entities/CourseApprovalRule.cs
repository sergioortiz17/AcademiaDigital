namespace AcademiaDigital.Domain.Entities;

public class CourseApprovalRule
{
    public int Id { get; set; }
    public int StudyPlanCourseId { get; set; }
    public StudyPlanCourse StudyPlanCourse { get; set; } = null!;
    public decimal? MinimumRegularGrade { get; set; }
    public decimal? MinimumPromotionGrade { get; set; }
    public decimal? MinimumAttendancePercentage { get; set; }
    public bool RequiresFinalExam { get; set; } = true;
    public bool AllowsPromotion { get; set; }
    public string? PolicyJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
