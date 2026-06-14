using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Entities;

public class StudyPlan
{
    public int Id { get; set; }
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public StudyPlanStatus Status { get; set; } = StudyPlanStatus.Draft;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public ICollection<StudyPlanCourse> Courses { get; set; } = [];
    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];
    public ICollection<StudentStudyPlan> StudentStudyPlans { get; set; } = [];
}
