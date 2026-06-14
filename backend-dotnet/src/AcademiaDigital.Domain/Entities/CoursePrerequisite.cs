using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Entities;

public class CoursePrerequisite
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan StudyPlan { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int PrerequisiteCourseId { get; set; }
    public Course PrerequisiteCourse { get; set; } = null!;
    public PrerequisiteType PrerequisiteType { get; set; } = PrerequisiteType.Strict;
    public MinimumRequiredStatus MinimumRequiredStatus { get; set; } = MinimumRequiredStatus.Approved;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
