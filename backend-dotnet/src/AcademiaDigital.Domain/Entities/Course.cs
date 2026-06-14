namespace AcademiaDigital.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;

    public ICollection<StudyPlanCourse> StudyPlanCourses { get; set; } = [];
    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];
    public ICollection<CoursePrerequisite> IsPrerequisiteFor { get; set; } = [];
}
