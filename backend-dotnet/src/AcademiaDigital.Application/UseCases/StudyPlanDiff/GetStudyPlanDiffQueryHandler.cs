using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.StudyPlanDiff;

public sealed record GetStudyPlanDiffQuery(int StudyPlanAId, int StudyPlanBId);

/// <summary>
/// Diffs two already-persisted study plans of the SAME career. Backs
/// GET api/v1/study-plans/{planAId}/diff/{planBId}.
/// </summary>
public sealed class GetStudyPlanDiffQueryHandler(
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    ICoursePrerequisiteRepository prerequisiteRepository)
{
    public async Task<StudyPlanDiffDto> Handle(GetStudyPlanDiffQuery query, CancellationToken ct = default)
    {
        var planA = await studyPlanRepository.GetByIdAsync(query.StudyPlanAId, ct)
            ?? throw new KeyNotFoundException("Plan de estudios A no encontrado.");
        var planB = await studyPlanRepository.GetByIdAsync(query.StudyPlanBId, ct)
            ?? throw new KeyNotFoundException("Plan de estudios B no encontrado.");

        if (planA.CareerId != planB.CareerId)
            throw new InvalidOperationException("Ambos planes de estudios deben pertenecer a la misma carrera.");

        var snapshotsA = await BuildSnapshotsAsync(planA.Id, ct);
        var snapshotsB = await BuildSnapshotsAsync(planB.Id, ct);

        var diff = StudyPlanDiffCalculator.Compute(snapshotsA, snapshotsB);
        diff.StudyPlanAId = planA.Id;
        diff.StudyPlanBId = planB.Id;
        return diff;
    }

    private async Task<IReadOnlyList<PlanCourseSnapshot>> BuildSnapshotsAsync(int studyPlanId, CancellationToken ct)
    {
        var courses = await studyPlanCourseRepository.GetByStudyPlanIdAsync(studyPlanId, ct);
        var prerequisites = await prerequisiteRepository.GetByStudyPlanIdAsync(studyPlanId, ct);

        var prereqCodesByCourseId = prerequisites
            .Where(p => p.IsActive)
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.PrerequisiteCourse.Code).OrderBy(c => c).ToList());

        return courses
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
    }
}
