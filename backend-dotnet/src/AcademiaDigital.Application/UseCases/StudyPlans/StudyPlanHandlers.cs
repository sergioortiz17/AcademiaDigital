using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.StudyPlans;

public sealed record GetCareerStudyPlansQuery(int CareerId);
public sealed record CreateStudyPlanCommand(int CareerId, CreateStudyPlanRequest Request);
public sealed record UpdateStudyPlanCommand(int CareerId, int StudyPlanId, CreateStudyPlanRequest Request);
public sealed record ActivateStudyPlanCommand(int CareerId, int StudyPlanId);
public sealed record GetCareerStudyPlanGroupedQuery(int CareerId, int StudyPlanId);

public sealed class GetCareerStudyPlansQueryHandler(IStudyPlanRepository studyPlanRepository)
{
    public async Task<IReadOnlyList<StudyPlanDto>> Handle(GetCareerStudyPlansQuery query, CancellationToken ct = default)
    {
        var studyPlans = await studyPlanRepository.GetByCareerIdAsync(query.CareerId, ct);
        return studyPlans.Select(Map).ToList();
    }

    private static StudyPlanDto Map(StudyPlan studyPlan) => new()
    {
        Id = studyPlan.Id,
        CareerId = studyPlan.CareerId,
        Code = studyPlan.Code,
        Name = studyPlan.Name,
        VersionNumber = studyPlan.VersionNumber,
        Status = studyPlan.Status.ToString(),
        EffectiveFrom = studyPlan.EffectiveFrom,
        EffectiveTo = studyPlan.EffectiveTo,
        IsActive = studyPlan.IsActive
    };
}

public sealed class CreateStudyPlanCommandHandler(
    ICareerRepository careerRepository,
    IStudyPlanRepository studyPlanRepository)
{
    public async Task<StudyPlanDto> Handle(CreateStudyPlanCommand command, CancellationToken ct = default)
    {
        var career = await careerRepository.FindByIdAsync(command.CareerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");

        var studyPlan = StudyPlan.Create(
            career.Id,
            command.Request.Code,
            command.Request.Name,
            command.Request.VersionNumber,
            command.Request.EffectiveFrom,
            command.Request.EffectiveTo);

        var created = await studyPlanRepository.CreateAsync(studyPlan, ct);
        return Map(created);
    }

    private static StudyPlanDto Map(StudyPlan studyPlan) => new()
    {
        Id = studyPlan.Id,
        CareerId = studyPlan.CareerId,
        Code = studyPlan.Code,
        Name = studyPlan.Name,
        VersionNumber = studyPlan.VersionNumber,
        Status = studyPlan.Status.ToString(),
        EffectiveFrom = studyPlan.EffectiveFrom,
        EffectiveTo = studyPlan.EffectiveTo,
        IsActive = studyPlan.IsActive
    };
}

public sealed class UpdateStudyPlanCommandHandler(IStudyPlanRepository studyPlanRepository)
{
    public async Task Handle(UpdateStudyPlanCommand command, CancellationToken ct = default)
    {
        var studyPlan = await studyPlanRepository.GetByIdAsync(command.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Plan de estudios no encontrado.");

        if (studyPlan.CareerId != command.CareerId) throw new KeyNotFoundException("Plan de estudios no encontrado en esta carrera.");

        studyPlan.Update(
            command.Request.Code,
            command.Request.Name,
            command.Request.VersionNumber,
            command.Request.EffectiveFrom,
            command.Request.EffectiveTo);

        await studyPlanRepository.UpdateAsync(studyPlan, ct);
    }
}

public sealed class ActivateStudyPlanCommandHandler(IStudyPlanRepository studyPlanRepository)
{
    public async Task Handle(ActivateStudyPlanCommand command, CancellationToken ct = default)
    {
        var studyPlans = (await studyPlanRepository.GetByCareerIdAsync(command.CareerId, ct)).ToList();
        var target = studyPlans.FirstOrDefault(sp => sp.Id == command.StudyPlanId)
            ?? throw new KeyNotFoundException("Plan de estudios no encontrado en esta carrera.");

        foreach (var studyPlan in studyPlans)
        {
            if (studyPlan.Id == target.Id)
                studyPlan.Activate();
            else
                studyPlan.Archive();
            await studyPlanRepository.UpdateAsync(studyPlan, ct);
        }
    }
}

public sealed class GetCareerStudyPlanGroupedQueryHandler(
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    ICoursePrerequisiteRepository prerequisiteRepository)
{
    public async Task<CareerStudyPlanDto> Handle(GetCareerStudyPlanGroupedQuery query, CancellationToken ct = default)
    {
        var studyPlan = await studyPlanRepository.GetByIdAsync(query.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Plan de estudios no encontrado.");

        if (studyPlan.CareerId != query.CareerId) throw new KeyNotFoundException("Plan de estudios no encontrado en esta carrera.");

        var courses = await studyPlanCourseRepository.GetByStudyPlanIdAsync(studyPlan.Id, ct);
        var prerequisites = await prerequisiteRepository.GetByStudyPlanIdAsync(studyPlan.Id, ct);

        var prerequisitesByCourse = prerequisites
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => g.Select(MapPrerequisite).ToList());

        var years = courses
            .GroupBy(c => c.YearNumber)
            .OrderBy(g => g.Key)
            .Select(yearGroup => new StudyPlanYearDto
            {
                YearNumber = yearGroup.Key,
                Semesters = yearGroup
                    .GroupBy(c => c.Semester)
                    .OrderBy(g => g.Key)
                    .Select(semesterGroup => new StudyPlanSemesterDto
                    {
                        Semester = semesterGroup.Key,
                        Courses = semesterGroup
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new StudyPlanCourseDto
                            {
                                CourseId = c.CourseId,
                                StudyPlanCourseId = c.Id,
                                Code = c.Course.Code,
                                Name = c.Course.Name,
                                CourseType = c.CourseType?.Name ?? "NORMAL",
                                YearNumber = c.YearNumber,
                                Semester = c.Semester,
                                IsMandatory = c.IsMandatory,
                                Prerequisites = prerequisitesByCourse.TryGetValue(c.CourseId, out var values) ? values : []
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        return new CareerStudyPlanDto
        {
            CareerId = studyPlan.CareerId,
            CareerName = studyPlan.Career.Name,
            StudyPlanId = studyPlan.Id,
            StudyPlanName = studyPlan.Name,
            VersionNumber = studyPlan.VersionNumber,
            Years = years
        };
    }

    private static CoursePrerequisiteDto MapPrerequisite(CoursePrerequisite prerequisite) => new()
    {
        CourseId = prerequisite.CourseId,
        PrerequisiteCourseId = prerequisite.PrerequisiteCourseId,
        PrerequisiteCourseCode = prerequisite.PrerequisiteCourse.Code,
        PrerequisiteCourseName = prerequisite.PrerequisiteCourse.Name,
        PrerequisiteType = prerequisite.PrerequisiteType.ToString(),
        MinimumRequiredStatus = prerequisite.MinimumRequiredStatus.ToString()
    };
}
