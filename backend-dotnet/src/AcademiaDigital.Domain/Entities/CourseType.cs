namespace AcademiaDigital.Domain.Entities;

public class CourseType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<StudyPlanCourse> StudyPlanCourses { get; set; } = [];
}
