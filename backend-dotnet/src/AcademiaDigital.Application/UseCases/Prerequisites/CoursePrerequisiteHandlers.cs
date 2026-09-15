using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Prerequisites;

public sealed record GetCoursePrerequisitesQuery(int StudyPlanId, int CourseId);
public sealed record AddCoursePrerequisiteCommand(int StudyPlanId, int CourseId, AddCoursePrerequisiteRequest Request);
public sealed record RemoveCoursePrerequisiteCommand(int StudyPlanId, int CourseId, int PrerequisiteCourseId);

public sealed class GetCoursePrerequisitesQueryHandler(ICoursePrerequisiteRepository prerequisiteRepository)
{
    public async Task<IReadOnlyList<CoursePrerequisiteDto>> Handle(GetCoursePrerequisitesQuery query, CancellationToken ct = default)
    {
        var prerequisites = await prerequisiteRepository.GetByCourseAsync(query.StudyPlanId, query.CourseId, ct);
        return prerequisites.Select(Map).ToList();
    }

    private static CoursePrerequisiteDto Map(CoursePrerequisite prerequisite) => new()
    {
        CourseId = prerequisite.CourseId,
        PrerequisiteCourseId = prerequisite.PrerequisiteCourseId,
        PrerequisiteCourseCode = prerequisite.PrerequisiteCourse.Code,
        PrerequisiteCourseName = prerequisite.PrerequisiteCourse.Name,
        PrerequisiteType = prerequisite.PrerequisiteType.ToString(),
        MinimumRequiredStatus = prerequisite.MinimumRequiredStatus.ToString()
    };
}

public sealed class AddCoursePrerequisiteCommandHandler(
    IStudyPlanRepository studyPlanRepository,
    ICourseRepository courseRepository,
    ICoursePrerequisiteRepository prerequisiteRepository,
    PrerequisiteCycleValidator cycleValidator)
{
    public async Task<CoursePrerequisiteDto> Handle(AddCoursePrerequisiteCommand command, CancellationToken ct = default)
    {
        if (command.CourseId == command.Request.PrerequisiteCourseId)
            throw new InvalidOperationException("Una materia no puede ser correlativa de sí misma.");

        var studyPlan = await studyPlanRepository.GetByIdAsync(command.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Plan de estudios no encontrado.");

        var courseInCareer = await courseRepository.ExistsInCareerAsync(command.CourseId, studyPlan.CareerId, ct);
        var prerequisiteInCareer = await courseRepository.ExistsInCareerAsync(command.Request.PrerequisiteCourseId, studyPlan.CareerId, ct);
        if (!courseInCareer || !prerequisiteInCareer)
            throw new InvalidOperationException("Ambas materias deben pertenecer a la carrera del plan de estudios.");

        var exists = await prerequisiteRepository.ExistsAsync(command.StudyPlanId, command.CourseId, command.Request.PrerequisiteCourseId, ct);
        if (exists) throw new InvalidOperationException("La correlativa ya existe.");

        var currentPrerequisites = await prerequisiteRepository.GetByStudyPlanIdAsync(command.StudyPlanId, ct);
        if (cycleValidator.WouldCreateCycle(currentPrerequisites, command.CourseId, command.Request.PrerequisiteCourseId))
            throw new InvalidOperationException("La correlativa generaría un ciclo.");

        var prerequisite = CoursePrerequisite.Create(
            command.StudyPlanId,
            command.CourseId,
            command.Request.PrerequisiteCourseId,
            Enum.Parse<PrerequisiteType>(command.Request.PrerequisiteType, ignoreCase: true),
            Enum.Parse<MinimumRequiredStatus>(command.Request.MinimumRequiredStatus, ignoreCase: true));

        var created = await prerequisiteRepository.CreateAsync(prerequisite, ct);
        var hydrated = (await prerequisiteRepository.GetByCourseAsync(command.StudyPlanId, command.CourseId, ct))
            .First(p => p.PrerequisiteCourseId == created.PrerequisiteCourseId);
        return Map(hydrated);
    }

    private static CoursePrerequisiteDto Map(CoursePrerequisite prerequisite) => new()
    {
        CourseId = prerequisite.CourseId,
        PrerequisiteCourseId = prerequisite.PrerequisiteCourseId,
        PrerequisiteCourseCode = prerequisite.PrerequisiteCourse.Code,
        PrerequisiteCourseName = prerequisite.PrerequisiteCourse.Name,
        PrerequisiteType = prerequisite.PrerequisiteType.ToString(),
        MinimumRequiredStatus = prerequisite.MinimumRequiredStatus.ToString()
    };
}

public sealed class RemoveCoursePrerequisiteCommandHandler(ICoursePrerequisiteRepository prerequisiteRepository)
{
    public async Task Handle(RemoveCoursePrerequisiteCommand command, CancellationToken ct = default)
    {
        await prerequisiteRepository.RemoveAsync(command.StudyPlanId, command.CourseId, command.PrerequisiteCourseId, ct);
    }
}
