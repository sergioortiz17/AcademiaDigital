namespace AcademiaDigital.Application.Dtos;

/// <summary>
/// Structured diff between two study plans' courses, git-diff style: courses only in B are
/// "added" (green), courses only in A are "removed" (red), courses in both with at least one
/// different attribute are "modified" (old value red / new value green per changed field).
/// Courses identical in both plans are not itemized, only counted (mirrors how `git diff` omits
/// unchanged context in compact mode).
///
/// Used by two endpoints:
///  - GET  api/v1/study-plans/{planAId}/diff/{planBId}                (two persisted plans)
///  - POST api/v1/careers/{careerId}/study-plans/{planId}/diff-preview (persisted plan vs an
///    unsaved CSV, planBId is null since B was never persisted)
/// </summary>
public sealed class StudyPlanDiffDto
{
    public int StudyPlanAId { get; set; }
    public int? StudyPlanBId { get; set; }
    public IReadOnlyList<CourseDiffItemDto> AddedCourses { get; set; } = [];
    public IReadOnlyList<CourseDiffItemDto> RemovedCourses { get; set; } = [];
    public IReadOnlyList<ModifiedCourseDiffDto> ModifiedCourses { get; set; } = [];
    public int UnchangedCourseCount { get; set; }
}

public sealed class CourseDiffItemDto
{
    public string CourseCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int YearNumber { get; set; }
    public int Semester { get; set; }
    public string? CourseTypeCode { get; set; }
    public int? WorkloadHours { get; set; }
    public bool IsMandatory { get; set; }
    public IReadOnlyList<string> Prerequisites { get; set; } = [];
}

public sealed class ModifiedCourseDiffDto
{
    public string CourseCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public IReadOnlyList<FieldChangeDto> FieldChanges { get; set; } = [];
    public PrerequisiteChangesDto? PrerequisiteChanges { get; set; }
}

public sealed class FieldChangeDto
{
    public string Field { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public sealed class PrerequisiteChangesDto
{
    public IReadOnlyList<string> Added { get; set; } = [];
    public IReadOnlyList<string> Removed { get; set; } = [];
}
