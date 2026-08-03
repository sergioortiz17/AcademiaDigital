using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.StudyPlanImport;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.StudyPlanDiff;

public sealed record PreviewStudyPlanDiffCommand(int CareerId, int StudyPlanId, Stream CsvContent);

/// <summary>
/// Diffs a NOT-YET-IMPORTED CSV against an existing, persisted study plan of the same career —
/// nothing is written to the DB. Backs
/// POST api/v1/careers/{careerId}/study-plans/{planId}/diff-preview, used by the admin UI to show
/// "what would change" before confirming an import.
/// </summary>
public sealed class PreviewStudyPlanDiffCommandHandler(
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    ICoursePrerequisiteRepository prerequisiteRepository,
    ICourseTypeRepository courseTypeRepository,
    PrerequisiteCycleValidator cycleValidator)
{
    public async Task<PreviewStudyPlanDiffResult> Handle(PreviewStudyPlanDiffCommand command, CancellationToken ct = default)
    {
        var plan = await studyPlanRepository.GetByIdAsync(command.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Study plan not found.");
        if (plan.CareerId != command.CareerId)
            throw new KeyNotFoundException("Study plan not found in this career.");

        var parseResult = await StudyPlanCsvParser.ParseAndValidateAsync(command.CsvContent, courseTypeRepository, cycleValidator, ct);
        if (!parseResult.Success)
            return PreviewStudyPlanDiffResult.Failed(parseResult.Errors);

        var prereqsByCode = parseResult.Rows.ToDictionary(
            r => r.CourseCode,
            r => (IReadOnlyList<string>)r.PrerequisiteCourseCodes.OrderBy(c => c).ToList());

        var snapshotsB = parseResult.Rows
            .Select(r => new PlanCourseSnapshot(
                r.CourseCode,
                r.Name,
                r.YearNumber,
                r.Semester,
                r.CourseTypeCode,
                r.WorkloadHours,
                r.IsMandatory,
                prereqsByCode[r.CourseCode]))
            .ToList();

        var existingCourses = await studyPlanCourseRepository.GetByStudyPlanIdAsync(plan.Id, ct);
        var existingPrerequisites = await prerequisiteRepository.GetByStudyPlanIdAsync(plan.Id, ct);
        var prereqCodesByCourseId = existingPrerequisites
            .Where(p => p.IsActive)
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.PrerequisiteCourse.Code).OrderBy(c => c).ToList());

        var snapshotsA = existingCourses
            .Select(c => new PlanCourseSnapshot(
                c.Course.Code,
                c.Course.Name,
                c.YearNumber,
                c.Semester,
                c.CourseType?.Code,
                c.WorkloadHours,
                c.IsMandatory,
                prereqCodesByCourseId.TryGetValue(c.CourseId, out var codes) ? codes : []))
            .ToList();

        var diff = StudyPlanDiffCalculator.Compute(snapshotsA, snapshotsB);
        diff.StudyPlanAId = plan.Id;
        diff.StudyPlanBId = null;
        return PreviewStudyPlanDiffResult.Succeeded(diff);
    }
}

public sealed class PreviewStudyPlanDiffResult
{
    public bool Success { get; init; }
    public StudyPlanDiffDto? Diff { get; init; }
    public IReadOnlyList<CsvRowError> Errors { get; init; } = [];

    public static PreviewStudyPlanDiffResult Failed(IReadOnlyList<CsvRowError> errors) => new()
    {
        Success = false,
        Errors = errors
    };

    public static PreviewStudyPlanDiffResult Succeeded(StudyPlanDiffDto diff) => new()
    {
        Success = true,
        Diff = diff
    };
}
