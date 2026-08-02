namespace AcademiaDigital.Application.UseCases.CareerImport;

/// <summary>
/// Error found while validating a single row of the career-import CSV.
/// Row 1 is the header; data rows start at 2 (matches what a spreadsheet app would show).
/// Row 0 is used for file-level errors (missing columns, empty file, etc.).
/// </summary>
public sealed record CsvRowError(int Row, string Error);

public sealed class ImportCareerCsvResult
{
    public bool Success { get; init; }
    public int? CareerId { get; init; }
    public int? StudyPlanId { get; init; }
    public int CoursesCreated { get; init; }
    public int PrerequisitesCreated { get; init; }
    public IReadOnlyList<CsvRowError> Errors { get; init; } = [];

    public static ImportCareerCsvResult Failed(IReadOnlyList<CsvRowError> errors) => new()
    {
        Success = false,
        Errors = errors
    };

    public static ImportCareerCsvResult Succeeded(int careerId, int studyPlanId, int coursesCreated, int prerequisitesCreated) => new()
    {
        Success = true,
        CareerId = careerId,
        StudyPlanId = studyPlanId,
        CoursesCreated = coursesCreated,
        PrerequisitesCreated = prerequisitesCreated
    };
}

/// <summary>
/// A single, already-validated row of the CSV, with everything parsed to its final type.
/// </summary>
internal sealed class CareerImportRow
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
