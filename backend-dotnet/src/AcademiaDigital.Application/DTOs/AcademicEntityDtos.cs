namespace AcademiaDigital.Application.Dtos;

public sealed class StudyPlanDto
{
    public int Id { get; set; }
    public int CareerId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string Status { get; set; } = null!;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CourseDto
{
    public int Id { get; set; }
    public int CareerId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StudyPlanCourseDetailDto
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public int YearNumber { get; set; }
    public int Semester { get; set; }
    public int SortOrder { get; set; }
    public bool IsMandatory { get; set; }
    public decimal? Credits { get; set; }
    public int? WorkloadHours { get; set; }
    public string? CourseType { get; set; }
}
