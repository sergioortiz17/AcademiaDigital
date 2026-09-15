using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public sealed record AttendanceRosterRow(
    long EnrollmentId,
    long StudentId,
    string StudentName,
    string LegajoNumber,
    string Dni);

public interface IAttendanceRepository
{
    Task<bool> CanTeacherManagePositionAsync(long userId, int teachingPositionId, DateOnly onDate, CancellationToken ct = default);
    Task<bool> CanTeacherManageSessionAsync(long userId, long sessionId, CancellationToken ct = default);
    Task<bool> CanTeacherViewStudentAsync(
        long userId,
        long studentId,
        int? courseId,
        int? commissionId,
        CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceSession>> GetSessionsAsync(
        int? academicYear,
        int? courseId,
        int? commissionId,
        long? teacherUserId,
        CancellationToken ct = default);
    Task<AttendanceSession?> FindSessionAsync(long sessionId, CancellationToken ct = default);
    Task<AttendanceSession?> FindSessionForUpdateAsync(long sessionId, CancellationToken ct = default);
    Task<(AttendanceSession Session, bool Created)> CreateIdempotentAsync(AttendanceSession session, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRosterRow>> GetRosterAsync(AttendanceSession session, CancellationToken ct = default);
    Task SaveRecordsAsync(AttendanceSession session, IReadOnlyList<AttendanceRecord> records, CancellationToken ct = default);
    Task SaveSessionAsync(AttendanceSession session, CancellationToken ct = default);
    Task<AttendanceRecord?> FindRecordForUpdateAsync(long recordId, CancellationToken ct = default);
    Task SaveJustificationAsync(AttendanceRecord record, AttendanceJustification justification, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecord>> GetStudentRecordsAsync(
        long studentId,
        int? courseId,
        int? commissionId,
        long? teacherUserId,
        CancellationToken ct = default);
}
