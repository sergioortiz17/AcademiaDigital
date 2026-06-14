namespace AcademiaDigital.Application.Dtos;

public sealed class CareerStudyPlanDto
{
    public long CareerId { get; set; }
    public string CareerName { get; set; } = null!;
    public long StudyPlanId { get; set; }
    public string StudyPlanName { get; set; } = null!;
    public int VersionNumber { get; set; }
    public IReadOnlyList<StudyPlanYearDto> Years { get; set; } = [];
}

public sealed class StudyPlanYearDto
{
    public int YearNumber { get; set; }
    public IReadOnlyList<StudyPlanSemesterDto> Semesters { get; set; } = [];
}

public sealed class StudyPlanSemesterDto
{
    public int Semester { get; set; }
    public IReadOnlyList<StudyPlanCourseDto> Courses { get; set; } = [];
}

public sealed class StudyPlanCourseDto
{
    public long CourseId { get; set; }
    public long StudyPlanCourseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string CourseType { get; set; } = null!;
    public int YearNumber { get; set; }
    public int Semester { get; set; }
    public bool IsMandatory { get; set; }
    public IReadOnlyList<CoursePrerequisiteDto> Prerequisites { get; set; } = [];
}

public sealed class CoursePrerequisiteDto
{
    public long CourseId { get; set; }
    public long PrerequisiteCourseId { get; set; }
    public string PrerequisiteCourseCode { get; set; } = null!;
    public string PrerequisiteCourseName { get; set; } = null!;
    public string PrerequisiteType { get; set; } = null!;
    public string MinimumRequiredStatus { get; set; } = null!;
}

public sealed class EligibleCourseDto
{
    public long CourseId { get; set; }
    public long StudyPlanCourseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int YearNumber { get; set; }
    public int Semester { get; set; }
    public string EligibilityStatus { get; set; } = null!;
    public IReadOnlyList<MissingPrerequisiteDto> MissingPrerequisites { get; set; } = [];
}

public sealed class MissingPrerequisiteDto
{
    public long CourseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PrerequisiteType { get; set; } = null!;
    public string RequiredStatus { get; set; } = null!;
    public string? CurrentStatus { get; set; }
}

public sealed class StudentAcademicProgressDto
{
    public long StudentId { get; set; }
    public long CareerId { get; set; }
    public string CareerName { get; set; } = null!;
    public long StudyPlanId { get; set; }
    public string StudyPlanName { get; set; } = null!;
    public int TotalCourses { get; set; }
    public int ApprovedCourses { get; set; }
    public int InProgressCourses { get; set; }
    public int PendingCourses { get; set; }
    public decimal ProgressPercentage { get; set; }
    public IReadOnlyList<StudentCourseProgressDto> Courses { get; set; } = [];
}

public sealed class StudentCourseProgressDto
{
    public long CourseId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int YearNumber { get; set; }
    public int Semester { get; set; }
    public string AcademicStatus { get; set; } = null!;
    public decimal? FinalGrade { get; set; }
    public int? AcademicYear { get; set; }
}
