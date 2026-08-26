using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class AttendanceRepository(AppDbContext db) : IAttendanceRepository
{
    public Task<bool> CanTeacherManagePositionAsync(long userId, int teachingPositionId, DateOnly onDate, CancellationToken ct = default)
        => db.TeacherAssignments.AsNoTracking().AnyAsync(assignment =>
            assignment.Teacher.UserId == userId
            && assignment.TeachingPositionId == teachingPositionId
            && assignment.StartedOn <= onDate
            && (!assignment.EndedOn.HasValue || assignment.EndedOn.Value >= onDate), ct);

    public Task<bool> CanTeacherManageSessionAsync(long userId, long sessionId, CancellationToken ct = default)
        => db.AttendanceSessions.AsNoTracking().AnyAsync(session => session.Id == sessionId
            && db.TeacherAssignments.Any(assignment =>
                assignment.Teacher.UserId == userId
                && assignment.TeachingPosition.CourseId == session.CourseId
                && assignment.TeachingPosition.CommissionId == session.CommissionId
                && assignment.TeachingPosition.AcademicYear == session.AcademicYear
                && assignment.TeachingPosition.Semester == session.Semester
                && assignment.StartedOn <= session.SessionDate
                && (!assignment.EndedOn.HasValue || assignment.EndedOn.Value >= session.SessionDate)), ct);

    public Task<bool> CanTeacherViewStudentAsync(
        long userId,
        long studentId,
        int? courseId,
        int? commissionId,
        CancellationToken ct = default)
        => db.AttendanceRecords.AsNoTracking().AnyAsync(record =>
            record.StudentId == studentId
            && record.AttendanceSession.Status == AttendanceSessionStatus.Closed
            && (!courseId.HasValue || record.AttendanceSession.CourseId == courseId)
            && (!commissionId.HasValue || record.AttendanceSession.CommissionId == commissionId)
            && db.TeacherAssignments.Any(assignment =>
                assignment.Teacher.UserId == userId
                && assignment.TeachingPosition.CourseId == record.AttendanceSession.CourseId
                && assignment.TeachingPosition.CommissionId == record.AttendanceSession.CommissionId
                && assignment.TeachingPosition.AcademicYear == record.AttendanceSession.AcademicYear
                && assignment.TeachingPosition.Semester == record.AttendanceSession.Semester
                && assignment.StartedOn <= record.AttendanceSession.SessionDate
                && (!assignment.EndedOn.HasValue || assignment.EndedOn.Value >= record.AttendanceSession.SessionDate)), ct);

    public async Task<IReadOnlyList<AttendanceSession>> GetSessionsAsync(
        int? academicYear,
        int? courseId,
        int? commissionId,
        long? teacherUserId,
        CancellationToken ct = default)
    {
        var query = Details();
        if (academicYear.HasValue) query = query.Where(session => session.AcademicYear == academicYear);
        if (courseId.HasValue) query = query.Where(session => session.CourseId == courseId);
        if (commissionId.HasValue) query = query.Where(session => session.CommissionId == commissionId);
        if (teacherUserId.HasValue)
            query = query.Where(session => db.TeacherAssignments.Any(assignment =>
                assignment.Teacher.UserId == teacherUserId
                && assignment.TeachingPosition.CourseId == session.CourseId
                && assignment.TeachingPosition.CommissionId == session.CommissionId
                && assignment.TeachingPosition.AcademicYear == session.AcademicYear
                && assignment.TeachingPosition.Semester == session.Semester
                && assignment.StartedOn <= session.SessionDate
                && (!assignment.EndedOn.HasValue || assignment.EndedOn.Value >= session.SessionDate)));
        return await query.OrderByDescending(session => session.SessionDate)
            .ThenByDescending(session => session.StartTime)
            .ToArrayAsync(ct);
    }

    public Task<AttendanceSession?> FindSessionAsync(long sessionId, CancellationToken ct = default)
        => Details().SingleOrDefaultAsync(session => session.Id == sessionId, ct);

    public async Task<AttendanceSession?> FindSessionForUpdateAsync(long sessionId, CancellationToken ct = default)
        => await db.AttendanceSessions
            .FromSqlInterpolated($"SELECT * FROM [AttendanceSessions] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {sessionId}")
            .SingleOrDefaultAsync(ct);

    public async Task<(AttendanceSession Session, bool Created)> CreateIdempotentAsync(
        AttendanceSession session,
        CancellationToken ct = default)
    {
        _ = await db.TeachingPositions
            .FromSqlInterpolated($"SELECT * FROM [TeachingPositions] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {session.TeachingPositionId}")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Teaching position not found.");
        var existing = await db.AttendanceSessions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdempotencyKey == session.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.TeachingPositionId != session.TeachingPositionId
                || existing.SessionDate != session.SessionDate
                || existing.StartTime != session.StartTime
                || existing.EndTime != session.EndTime
                || existing.Scope != session.Scope
                || existing.Units != session.Units)
                throw new InvalidOperationException("The idempotency key was already used with a different attendance session.");
            return ((await Details().SingleAsync(item => item.Id == existing.Id, ct)), false);
        }

        var duplicateOffering = await db.AttendanceSessions.AsNoTracking().AnyAsync(item =>
            item.CourseId == session.CourseId
            && item.CommissionId == session.CommissionId
            && item.AcademicYear == session.AcademicYear
            && item.Semester == session.Semester
            && item.SessionDate == session.SessionDate
            && item.StartTime == session.StartTime
            && item.Scope == session.Scope, ct);
        if (duplicateOffering)
            throw new InvalidOperationException("An attendance session already exists for this course, commission and time.");

        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return ((await Details().SingleAsync(item => item.Id == session.Id, ct)), true);
    }

    public async Task<IReadOnlyList<AttendanceRosterRow>> GetRosterAsync(
        AttendanceSession session,
        CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Where(enrollment => enrollment.CourseId == session.CourseId
                && enrollment.AcademicYear == session.AcademicYear
                && enrollment.Semester == session.Semester
                && enrollment.Status != EnrollmentStatus.Withdrawn
                && (enrollment.TeachingPositionId == session.TeachingPositionId
                    || (enrollment.TeachingPositionId == null && db.StudentAcademicAssignments.Any(assignment =>
                        assignment.StudentCareerId == enrollment.StudentCareerId
                        && assignment.CommissionId == session.CommissionId
                        && assignment.AcademicYear == session.AcademicYear))))
            .OrderBy(enrollment => enrollment.Student.User.LastName)
            .ThenBy(enrollment => enrollment.Student.User.Username)
            .Select(enrollment => new AttendanceRosterRow(
                enrollment.Id,
                enrollment.StudentId,
                (enrollment.Student.User.Username + " " + enrollment.Student.User.LastName).Trim(),
                enrollment.Student.LegajoNumber,
                enrollment.Student.User.Dni ?? string.Empty))
            .ToArrayAsync(ct);

    public async Task SaveRecordsAsync(
        AttendanceSession session,
        IReadOnlyList<AttendanceRecord> records,
        CancellationToken ct = default)
    {
        var enrollmentIds = records.Select(record => record.EnrollmentId).ToArray();
        var existing = await db.AttendanceRecords
            .Where(record => record.AttendanceSessionId == session.Id && enrollmentIds.Contains(record.EnrollmentId))
            .ToDictionaryAsync(record => record.EnrollmentId, ct);
        foreach (var record in records)
        {
            if (existing.TryGetValue(record.EnrollmentId, out var current))
            {
                if (current.Status == AttendanceRecordStatus.Justified)
                    throw new InvalidOperationException("A justified attendance record cannot be overwritten by bulk loading.");
                current.Status = record.Status;
                current.Notes = record.Notes;
                current.UpdatedAt = record.UpdatedAt;
                current.UpdatedByUserId = record.UpdatedByUserId;
            }
            else
            {
                db.AttendanceRecords.Add(record);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveSessionAsync(AttendanceSession session, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task<AttendanceRecord?> FindRecordForUpdateAsync(long recordId, CancellationToken ct = default)
    {
        var record = await db.AttendanceRecords
            .FromSqlInterpolated($"SELECT * FROM [AttendanceRecords] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {recordId}")
            .SingleOrDefaultAsync(ct);
        if (record is not null)
            await db.Entry(record).Reference(item => item.AttendanceSession).LoadAsync(ct);
        return record;
    }

    public async Task SaveJustificationAsync(
        AttendanceRecord record,
        AttendanceJustification justification,
        CancellationToken ct = default)
    {
        var current = await db.AttendanceJustifications
            .Where(item => item.AttendanceRecordId == record.Id && item.IsCurrent)
            .ToArrayAsync(ct);
        foreach (var item in current) item.IsCurrent = false;
        record.Status = AttendanceRecordStatus.Justified;
        record.UpdatedAt = justification.CreatedAt;
        record.UpdatedByUserId = justification.CreatedByUserId;
        db.AttendanceJustifications.Add(justification);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetStudentRecordsAsync(
        long studentId,
        int? courseId,
        int? commissionId,
        long? teacherUserId,
        CancellationToken ct = default)
    {
        var query = db.AttendanceRecords.AsNoTracking()
            .Include(record => record.AttendanceSession).ThenInclude(session => session.Course)
            .Include(record => record.AttendanceSession).ThenInclude(session => session.Commission)
            .Include(record => record.Enrollment).ThenInclude(enrollment => enrollment.StudyPlanCourse)!.ThenInclude(course => course!.ApprovalRule)
            .Include(record => record.Justifications.Where(justification => justification.IsCurrent))
            .Where(record => record.StudentId == studentId
                && record.AttendanceSession.Status == AttendanceSessionStatus.Closed);
        if (courseId.HasValue) query = query.Where(record => record.AttendanceSession.CourseId == courseId);
        if (commissionId.HasValue) query = query.Where(record => record.AttendanceSession.CommissionId == commissionId);
        if (teacherUserId.HasValue)
            query = query.Where(record => db.TeacherAssignments.Any(assignment =>
                assignment.Teacher.UserId == teacherUserId
                && assignment.TeachingPosition.CourseId == record.AttendanceSession.CourseId
                && assignment.TeachingPosition.CommissionId == record.AttendanceSession.CommissionId
                && assignment.TeachingPosition.AcademicYear == record.AttendanceSession.AcademicYear
                && assignment.TeachingPosition.Semester == record.AttendanceSession.Semester
                && assignment.StartedOn <= record.AttendanceSession.SessionDate
                && (!assignment.EndedOn.HasValue || assignment.EndedOn.Value >= record.AttendanceSession.SessionDate)));
        return await query.OrderBy(record => record.AttendanceSession.SessionDate).ToArrayAsync(ct);
    }

    private IQueryable<AttendanceSession> Details()
        => db.AttendanceSessions.AsNoTracking()
            .Include(session => session.Course)
            .Include(session => session.Commission)
            .Include(session => session.Records).ThenInclude(record => record.Student).ThenInclude(student => student.User)
            .Include(session => session.Records).ThenInclude(record => record.Enrollment)
            .Include(session => session.Records).ThenInclude(record => record.Justifications.Where(justification => justification.IsCurrent))
            .Include(session => session.Reopenings);
}
