namespace AcademiaDigital.Application.UseCases.StudyPlanImport;

/// <summary>
/// Error found while validating a single row of a study-plan CSV.
/// Row 1 is the header; data rows start at 2 (matches what a spreadsheet app would show).
/// Row 0 is used for file-level errors (missing columns, empty file, etc.).
/// </summary>
public sealed record CsvRowError(int Row, string Error);

public sealed class ImportStudyPlanCsvResult
{
    public bool Success { get; init; }
    public int? StudyPlanId { get; init; }
    public int CoursesCreated { get; init; }
    public int PrerequisitesCreated { get; init; }
    public IReadOnlyList<CsvRowError> Errors { get; init; } = [];

    public static ImportStudyPlanCsvResult Failed(IReadOnlyList<CsvRowError> errors) => new()
    {
        Success = false,
        Errors = errors
    };

    public static ImportStudyPlanCsvResult Succeeded(int studyPlanId, int coursesCreated, int prerequisitesCreated) => new()
    {
        Success = true,
        StudyPlanId = studyPlanId,
        CoursesCreated = coursesCreated,
        PrerequisitesCreated = prerequisitesCreated
    };
}

/// <summary>
/// A single, already-validated row of the study-plan CSV, with everything parsed to its final type.
/// Shared between the import handler and the diff-preview handler.
/// </summary>
public sealed class StudyPlanCsvRow
{
    public int RowNumber { get; init; }
    public int SortOrder { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int YearNumber { get; init; }
    public int Semester { get; init; }
    public string? CourseTypeCode { get; init; }
    public int? WorkloadHours { get; init; }
    public bool IsMandatory { get; init; } = true;
    public IReadOnlyList<string> PrerequisiteCourseCodes { get; init; } = [];
}
