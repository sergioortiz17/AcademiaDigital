using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Teachers;

public sealed record AssignCoursesToTeacherCommand(
    long TeacherId,
    int DivisionId,
    IReadOnlyCollection<int> CourseIds,
    PositionType PositionType,
    DateOnly StartedOn,
    int MaxStudents,
    string? Reason,
    long ActorUserId);

public sealed record CourseAssignmentFailure(int CourseId, string Reason);

public sealed record AssignCoursesToTeacherResult(
    IReadOnlyList<TeacherAssignmentDto> Assigned,
    IReadOnlyList<CourseAssignmentFailure> Failed);

/// <summary>
/// Asigna varias materias a un docente en un paso: para cada materia del curso busca la
/// comisión (misma materia, curso, año académico y cargo). Si existe y está vacante la usa; si no
/// existe la crea con el cuatrimestre (o anual) que indica el plan de estudios vigente de la carrera.
/// Cada materia se resuelve por separado: un fallo no impide asignar las demás.
/// </summary>
public sealed class AssignCoursesToTeacherHandler(
    ITeacherRepository teachers,
    IDivisionRepository divisions,
    IStudyPlanRepository studyPlans,
    IStudyPlanCourseRepository studyPlanCourses,
    ICourseSectionRepository sections,
    CreateCourseSectionCommandHandler createSection,
    AssignTeacherCommandHandler assignTeacher)
{
    public const int MaxPerRequest = 50;

    public async Task<AssignCoursesToTeacherResult> Handle(
        AssignCoursesToTeacherCommand command,
        CancellationToken ct = default)
    {
        var courseIds = command.CourseIds.Distinct().ToArray();
        if (courseIds.Length == 0)
            throw new ArgumentException("Selecciona al menos una materia.");
        if (courseIds.Length > MaxPerRequest)
            throw new ArgumentException($"Se pueden asignar hasta {MaxPerRequest} materias por vez.");

        var teacher = await teachers.FindByIdAsync(command.TeacherId, ct)
            ?? throw new KeyNotFoundException("Docente no encontrado.");
        if (!teacher.IsActive || !teacher.User.IsActive)
            throw new InvalidOperationException("El docente está inactivo.");
        var division = await divisions.FindByIdAsync(command.DivisionId, ct)
            ?? throw new KeyNotFoundException("Curso no encontrado.");
        if (!division.IsActive)
            throw new InvalidOperationException("El curso está inactivo.");
        // Se valida antes de crear comisiones, para no dejar comisiones vacías si la asignación falla.
        if (command.StartedOn.Year != division.AcademicYear)
            throw new ArgumentException(
                $"La fecha de inicio debe pertenecer al año académico del curso ({division.AcademicYear}).");

        var plan = await studyPlans.GetActiveByCareerIdAsync(division.CareerId, ct)
            ?? throw new InvalidOperationException("La carrera no tiene un plan de estudios vigente.");
        var planCourses = (await studyPlanCourses.GetByStudyPlanIdAsync(plan.Id, ct))
            .Where(c => c.IsActive)
            .GroupBy(c => c.CourseId)
            .ToDictionary(g => g.Key, g => g.First());

        var assigned = new List<TeacherAssignmentDto>();
        var failed = new List<CourseAssignmentFailure>();

        foreach (var courseId in courseIds)
        {
            if (!planCourses.TryGetValue(courseId, out var planCourse))
            {
                failed.Add(new(courseId, "La materia no está en el plan de estudios vigente de la carrera."));
                continue;
            }

            try
            {
                var sectionId = await ResolveSectionAsync(command, division, planCourse, ct);
                assigned.Add(await assignTeacher.Handle(new AssignTeacherCommand(
                    command.TeacherId, sectionId, command.StartedOn, command.Reason, command.ActorUserId), ct));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException)
            {
                failed.Add(new(courseId, ex.Message));
            }
        }

        return new AssignCoursesToTeacherResult(assigned, failed);
    }

    /// <summary>Devuelve la comisión vacante a usar, creándola si todavía no existe.</summary>
    private async Task<int> ResolveSectionAsync(
        AssignCoursesToTeacherCommand command,
        Division division,
        StudyPlanCourse planCourse,
        CancellationToken ct)
    {
        var existing = (await sections.GetByCourseAsync(planCourse.CourseId, ct))
            .Where(s => s.IsActive
                && s.DivisionId == division.Id
                && s.AcademicYear == division.AcademicYear
                && s.PositionType == command.PositionType)
            .ToList();

        var vacant = existing.FirstOrDefault(s => s.IsVacant && !s.TeacherId.HasValue);
        if (vacant is not null) return vacant.Id;

        var taken = existing.FirstOrDefault();
        if (taken is not null)
        {
            if (taken.TeacherId == command.TeacherId)
                throw new InvalidOperationException("El docente ya tiene asignada esta materia en el curso.");
            throw new InvalidOperationException("La comisión de esta materia en el curso ya tiene otro docente.");
        }

        var created = await createSection.Handle(new CreateCourseSectionCommand(
            planCourse.CourseId,
            division.Id,
            division.AcademicYear,
            planCourse.IsAnnual ? 1 : planCourse.Semester,
            command.PositionType,
            command.MaxStudents,
            planCourse.IsAnnual), ct);
        return created.Id;
    }
}
