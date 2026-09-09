namespace AcademiaDigital.Application.UseCases.StudyPlanDiff;

/// <summary>
/// Plan-agnostic view of "one course as it appears in one study plan", used as the common input
/// to <see cref="StudyPlanDiffCalculator"/>. Built either from persisted StudyPlanCourse/
/// CoursePrerequisite rows (GetStudyPlanDiffQueryHandler) or from a parsed-but-unsaved CSV
/// (PreviewStudyPlanDiffCommandHandler) — the calculator itself never touches the DB.
/// </summary>
public sealed record PlanCourseSnapshot(
    string CourseCode,
    string Name,
    int YearNumber,
    int Semester,
    string? CourseTypeCode,
    int? WorkloadHours,
    bool IsMandatory,
    IReadOnlyList<string> Prerequisites);
