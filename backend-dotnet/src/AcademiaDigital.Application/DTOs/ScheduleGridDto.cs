namespace AcademiaDigital.Application.Dtos;

public sealed class ScheduleGridDto
{
    public int StudyPlanId { get; set; }
    public IReadOnlyList<ScheduleGridItemDto> Items { get; set; } = [];
}

public sealed class ScheduleGridItemDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int YearNumber { get; set; }
    public int Semester { get; set; }
}
