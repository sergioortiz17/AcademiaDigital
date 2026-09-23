using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Teachers;

public sealed record GetTeacherAssignmentsQuery(long TeacherId, bool IncludeEnded);
public sealed record GetMyTeacherAssignmentsQuery(long UserId, bool IncludeEnded);
public sealed record AssignTeacherCommand(
    long TeacherId,
    int CourseSectionId,
    DateOnly StartedOn,
    string? Reason,
    long ActorUserId);
public sealed record EndTeacherAssignmentCommand(
    long TeacherId,
    long AssignmentId,
    DateOnly EndedOn,
    string Reason,
    long ActorUserId);

public sealed record TeacherAssignmentDto(
    long Id,
    long TeacherId,
    string TeacherName,
    int CourseSectionId,
    int CourseId,
    string CourseCode,
    string CourseName,
    int? DivisionId,
    string? DivisionCode,
    string? DivisionName,
    int AcademicYear,
    int Semester,
    string PositionType,
    int MaxStudents,
    DateOnly StartedOn,
    DateOnly? EndedOn,
    bool IsCurrent,
    string? AssignmentReason,
    string? EndReason,
    long? AssignedByUserId,
    long? EndedByUserId,
    DateTime CreatedAt,
    DateTime? EndedAt);

public sealed class GetTeacherAssignmentsQueryHandler(
    ITeacherRepository teacherRepository,
    ITeacherAssignmentRepository assignmentRepository)
{
    public async Task<IReadOnlyList<TeacherAssignmentDto>> Handle(
        GetTeacherAssignmentsQuery query,
        CancellationToken ct = default)
    {
        _ = await teacherRepository.FindByIdAsync(query.TeacherId, ct)
            ?? throw new KeyNotFoundException("Docente no encontrado.");
        return (await assignmentRepository.GetByTeacherAsync(query.TeacherId, query.IncludeEnded, ct))
            .Select(TeacherAssignmentMapper.Map)
            .ToArray();
    }
}

public sealed class GetMyTeacherAssignmentsQueryHandler(
    ITeacherRepository teacherRepository,
    ITeacherAssignmentRepository assignmentRepository)
{
    public async Task<IReadOnlyList<TeacherAssignmentDto>> Handle(
        GetMyTeacherAssignmentsQuery query,
        CancellationToken ct = default)
    {
        var teacher = await teacherRepository.FindByUserIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("Perfil de docente no encontrado.");
        return (await assignmentRepository.GetByTeacherAsync(teacher.Id, query.IncludeEnded, ct))
            .Select(TeacherAssignmentMapper.Map)
            .ToArray();
    }
}

public sealed class AssignTeacherCommandHandler(
    ITeacherRepository teacherRepository,
    ICourseSectionRepository positionRepository,
    ITeacherAssignmentRepository assignmentRepository,
    ITeacherCareerRepository teacherCareerRepository,
    TeachingAssignmentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<TeacherAssignmentDto> Handle(
        AssignTeacherCommand command,
        CancellationToken ct = default)
    {
        var teacher = await teacherRepository.FindByIdAsync(command.TeacherId, ct)
            ?? throw new KeyNotFoundException("Docente no encontrado.");
        var position = await positionRepository.FindByIdAsync(command.CourseSectionId, ct)
            ?? throw new KeyNotFoundException("Cargo docente no encontrado.");
        policy.EnsureCanAssign(position, teacher, command.StartedOn);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var created = await unitOfWork.ExecuteInSerializableTransactionAsync(
            async transactionCt =>
            {
                var assignment = await assignmentRepository.AssignAsync(new TeacherAssignment
                {
                    TeacherId = command.TeacherId,
                    CourseSectionId = command.CourseSectionId,
                    StartedOn = command.StartedOn,
                    IsCurrent = true,
                    AssignmentReason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim(),
                    AssignedByUserId = command.ActorUserId,
                    CreatedAt = now
                }, transactionCt);
                // Simetría con StudentCareer: al asignar al profesor a una sección de una carrera con
                // la que aún no tenía vínculo, se auto-crea el TeacherCareer (idempotente, no bloqueante).
                await teacherCareerRepository.EnsureAsync(command.TeacherId, position.Course.CareerId, transactionCt);
                return assignment;
            }, ct);
        return TeacherAssignmentMapper.Map(created);
    }
}

public sealed class EndTeacherAssignmentCommandHandler(
    ITeacherAssignmentRepository repository,
    TeachingAssignmentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<TeacherAssignmentDto> Handle(
        EndTeacherAssignmentCommand command,
        CancellationToken ct = default)
    {
        var assignment = await repository.FindAsync(command.TeacherId, command.AssignmentId, ct)
            ?? throw new KeyNotFoundException("Asignación docente no encontrada.");
        policy.EnsureCanEnd(assignment, command.EndedOn, command.Reason);
        var ended = await unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCt => repository.EndAsync(
                command.TeacherId,
                command.AssignmentId,
                command.EndedOn,
                timeProvider.GetUtcNow().UtcDateTime,
                command.ActorUserId,
                command.Reason,
                transactionCt), ct);
        return TeacherAssignmentMapper.Map(ended);
    }
}

internal static class TeacherAssignmentMapper
{
    public static TeacherAssignmentDto Map(TeacherAssignment assignment) => new(
        assignment.Id,
        assignment.TeacherId,
        $"{assignment.Teacher.User.Username} {assignment.Teacher.User.LastName}".Trim(),
        assignment.CourseSectionId,
        assignment.CourseSection.CourseId,
        assignment.CourseSection.Course.Code,
        assignment.CourseSection.Course.Name,
        assignment.CourseSection.DivisionId,
        assignment.CourseSection.Division?.Code,
        assignment.CourseSection.Division?.Name,
        assignment.CourseSection.AcademicYear,
        assignment.CourseSection.Semester,
        assignment.CourseSection.PositionType.ToString(),
        assignment.CourseSection.MaxStudents,
        assignment.StartedOn,
        assignment.EndedOn,
        assignment.IsCurrent,
        assignment.AssignmentReason,
        assignment.EndReason,
        assignment.AssignedByUserId,
        assignment.EndedByUserId,
        assignment.CreatedAt,
        assignment.EndedAt);
}
