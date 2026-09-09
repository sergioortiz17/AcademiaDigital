using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ITeacherAssignmentRepository
{
    Task<IReadOnlyList<TeacherAssignment>> GetByTeacherAsync(
        long teacherId,
        bool includeEnded,
        CancellationToken ct = default);

    Task<bool> HasHistoryForPositionAsync(int teachingPositionId, CancellationToken ct = default);

    Task<TeacherAssignment?> FindAsync(
        long teacherId,
        long assignmentId,
        CancellationToken ct = default);

    Task<TeacherAssignment> AssignAsync(
        TeacherAssignment assignment,
        CancellationToken ct = default);

    Task<TeacherAssignment> EndAsync(
        long teacherId,
        long assignmentId,
        DateOnly endedOn,
        DateTime endedAt,
        long actorUserId,
        string reason,
        CancellationToken ct = default);
}
