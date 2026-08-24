using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Attendance;

public sealed record GetAttendanceSessionsQuery(
    int? AcademicYear,
    int? CourseId,
    int? CommissionId,
    long ActorUserId,
    bool IsAdmin);
public sealed record GetAttendanceSessionQuery(long SessionId, long ActorUserId, bool IsAdmin);
public sealed record CreateAttendanceSessionCommand(
    string IdempotencyKey,
    int TeachingPositionId,
    DateOnly SessionDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    AttendanceScope Scope,
    int Units,
    long ActorUserId,
    bool IsAdmin);
public sealed record AttendanceRecordInput(long EnrollmentId, AttendanceRecordStatus Status, string? Notes);
public sealed record SaveAttendanceRecordsCommand(
    long SessionId,
    IReadOnlyList<AttendanceRecordInput> Records,
    long ActorUserId,
    bool IsAdmin);
public sealed record CloseAttendanceSessionCommand(long SessionId, long ActorUserId, bool IsAdmin);
public sealed record ReopenAttendanceSessionCommand(long SessionId, string Reason, long ActorUserId);
public sealed record JustifyAttendanceRecordCommand(
    long RecordId,
    string Category,
    string Reason,
    string? EvidenceUrl,
    long ActorUserId);
public sealed record GetStudentAttendanceSummaryQuery(
    long StudentId,
    int? CourseId,
    int? CommissionId,
    long ActorUserId,
    bool IsAdmin);
public sealed record GetMyAttendanceSummaryQuery(
    long UserId,
    int? CourseId,
    int? CommissionId);
public sealed record ExportAttendanceSessionQuery(
    long SessionId,
    string Format,
    long ActorUserId,
    bool IsAdmin);

public sealed record AttendanceJustificationDto(
    long Id,
    string Category,
    string Reason,
    string? EvidenceUrl,
    DateTime CreatedAt,
    long CreatedByUserId);

public sealed record AttendanceRecordDto(
    long? Id,
    long EnrollmentId,
    long StudentId,
    string StudentName,
    string LegajoNumber,
    string Dni,
    AttendanceRecordStatus? Status,
    string? Notes,
    DateTime? UpdatedAt,
    AttendanceJustificationDto? Justification);

public sealed record AttendanceSessionDto(
    long Id,
    string IdempotencyKey,
    int TeachingPositionId,
    int CourseId,
    string CourseCode,
    string CourseName,
    int CommissionId,
    string CommissionCode,
    string CommissionName,
    int AcademicYear,
    int Semester,
    DateOnly SessionDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    AttendanceScope Scope,
    int Units,
    AttendanceSessionStatus Status,
    DateTime EditDeadlineUtc,
    bool IsAdministrativelyReopened,
    int RecordCount,
    int ReopeningCount,
    DateTime CreatedAt,
    long CreatedByUserId,
    DateTime? ClosedAt,
    long? ClosedByUserId);

public sealed record AttendanceSessionDetailDto(
    AttendanceSessionDto Session,
    IReadOnlyList<AttendanceRecordDto> Records);

public sealed record AttendanceSummaryItemDto(
    int CourseId,
    string CourseCode,
    string CourseName,
    int CommissionId,
    string CommissionCode,
    string CommissionName,
    int AcademicYear,
    int Semester,
    decimal? MinimumAttendancePercentage,
    decimal EarnedUnits,
    decimal PossibleUnits,
    decimal? AttendancePercentage,
    bool IsAtRisk,
    int PresentCount,
    int LateCount,
    int AbsentCount,
    int JustifiedCount);

public sealed record StudentAttendanceSummaryDto(
    long StudentId,
    string StudentName,
    string LegajoNumber,
    IReadOnlyList<AttendanceSummaryItemDto> Items);

public sealed class GetAttendanceSessionsQueryHandler(IAttendanceRepository repository)
{
    public async Task<IReadOnlyList<AttendanceSessionDto>> Handle(GetAttendanceSessionsQuery query, CancellationToken ct = default)
        => (await repository.GetSessionsAsync(
                query.AcademicYear,
                query.CourseId,
                query.CommissionId,
                query.IsAdmin ? null : query.ActorUserId,
                ct))
            .Select(AttendanceMapper.MapSession)
            .ToArray();
}

public sealed class GetAttendanceSessionQueryHandler(IAttendanceRepository repository)
{
    public async Task<AttendanceSessionDetailDto> Handle(GetAttendanceSessionQuery query, CancellationToken ct = default)
    {
        await AttendanceAuthorization.EnsureCanManageSession(repository, query.SessionId, query.ActorUserId, query.IsAdmin, ct);
        return await AttendanceMapper.LoadDetail(repository, query.SessionId, ct);
    }
}

public sealed class CreateAttendanceSessionCommandHandler(
    ITeachingPositionRepository positionRepository,
    IAttendanceRepository attendanceRepository,
    AttendancePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AttendanceSessionDto> Handle(CreateAttendanceSessionCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 100)
            throw new ArgumentException("A valid idempotency key of up to 100 characters is required.");
        var position = await positionRepository.FindByIdAsync(command.TeachingPositionId, ct)
            ?? throw new KeyNotFoundException("Teaching position not found.");
        await AttendanceAuthorization.EnsureCanManagePosition(
            attendanceRepository, command.TeachingPositionId, command.SessionDate,
            command.ActorUserId, command.IsAdmin, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var editDeadline = policy.EnsureCanCreateSession(
            position, command.SessionDate, command.StartTime, command.EndTime,
            command.Scope, command.Units, now);
        var result = await unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCt => attendanceRepository.CreateIdempotentAsync(new AttendanceSession
            {
                IdempotencyKey = command.IdempotencyKey.Trim(),
                TeachingPositionId = position.Id,
                CourseId = position.CourseId,
                CommissionId = position.CommissionId!.Value,
                AcademicYear = position.AcademicYear,
                Semester = position.Semester,
                SessionDate = command.SessionDate,
                StartTime = command.StartTime,
                EndTime = command.EndTime,
                Scope = command.Scope,
                Units = command.Units,
                Status = AttendanceSessionStatus.Open,
                EditDeadlineUtc = editDeadline,
                CreatedAt = now,
                CreatedByUserId = command.ActorUserId
            }, transactionCt), ct);
        return AttendanceMapper.MapSession(result.Session);
    }
}

public sealed class SaveAttendanceRecordsCommandHandler(
    IAttendanceRepository repository,
    AttendancePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AttendanceSessionDetailDto> Handle(SaveAttendanceRecordsCommand command, CancellationToken ct = default)
    {
        if (command.Records.Count == 0) throw new ArgumentException("At least one attendance record is required.");
        if (command.Records.Select(record => record.EnrollmentId).Distinct().Count() != command.Records.Count)
            throw new ArgumentException("An enrollment cannot appear more than once in a bulk attendance request.");
        foreach (var record in command.Records) policy.EnsureRecordStatusCanBeLoaded(record.Status);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            await AttendanceAuthorization.EnsureCanManageSession(
                repository, command.SessionId, command.ActorUserId, command.IsAdmin, transactionCt);
            var session = await repository.FindSessionForUpdateAsync(command.SessionId, transactionCt)
                ?? throw new KeyNotFoundException("Attendance session not found.");
            policy.EnsureEditable(session, now);
            var roster = await repository.GetRosterAsync(session, transactionCt);
            var rosterByEnrollment = roster.ToDictionary(row => row.EnrollmentId);
            if (command.Records.Any(record => !rosterByEnrollment.ContainsKey(record.EnrollmentId)))
                throw new ArgumentException("Every attendance record must belong to the session roster.");
            var records = command.Records.Select(input => new AttendanceRecord
            {
                AttendanceSessionId = session.Id,
                EnrollmentId = input.EnrollmentId,
                StudentId = rosterByEnrollment[input.EnrollmentId].StudentId,
                Status = input.Status,
                Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
                UpdatedAt = now,
                UpdatedByUserId = command.ActorUserId
            }).ToArray();
            await repository.SaveRecordsAsync(session, records, transactionCt);
            return true;
        }, ct);
        return await AttendanceMapper.LoadDetail(repository, command.SessionId, ct);
    }
}

public sealed class CloseAttendanceSessionCommandHandler(
    IAttendanceRepository repository,
    AttendancePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AttendanceSessionDto> Handle(CloseAttendanceSessionCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            await AttendanceAuthorization.EnsureCanManageSession(repository, command.SessionId, command.ActorUserId, command.IsAdmin, transactionCt);
            var session = await repository.FindSessionForUpdateAsync(command.SessionId, transactionCt)
                ?? throw new KeyNotFoundException("Attendance session not found.");
            policy.EnsureCanClose(session);
            session.Status = AttendanceSessionStatus.Closed;
            session.IsAdministrativelyReopened = false;
            session.ClosedAt = timeProvider.GetUtcNow().UtcDateTime;
            session.ClosedByUserId = command.ActorUserId;
            await repository.SaveSessionAsync(session, transactionCt);
            return true;
        }, ct);
        return AttendanceMapper.MapSession(await repository.FindSessionAsync(command.SessionId, ct)
            ?? throw new KeyNotFoundException("Attendance session not found."));
    }
}

public sealed class ReopenAttendanceSessionCommandHandler(
    IAttendanceRepository repository,
    AttendancePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AttendanceSessionDto> Handle(ReopenAttendanceSessionCommand command, CancellationToken ct = default)
    {
        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var session = await repository.FindSessionForUpdateAsync(command.SessionId, transactionCt)
                ?? throw new KeyNotFoundException("Attendance session not found.");
            policy.EnsureCanReopen(session, command.Reason);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            session.Status = AttendanceSessionStatus.Open;
            session.IsAdministrativelyReopened = true;
            session.ClosedAt = null;
            session.ClosedByUserId = null;
            session.Reopenings.Add(new AttendanceSessionReopening
            {
                Reason = command.Reason.Trim(),
                ReopenedAt = now,
                ReopenedByUserId = command.ActorUserId
            });
            await repository.SaveSessionAsync(session, transactionCt);
            return true;
        }, ct);
        return AttendanceMapper.MapSession(await repository.FindSessionAsync(command.SessionId, ct)
            ?? throw new KeyNotFoundException("Attendance session not found."));
    }
}

public sealed class JustifyAttendanceRecordCommandHandler(
    IAttendanceRepository repository,
    AttendancePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AttendanceJustificationDto> Handle(JustifyAttendanceRecordCommand command, CancellationToken ct = default)
    {
        var justification = await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var record = await repository.FindRecordForUpdateAsync(command.RecordId, transactionCt)
                ?? throw new KeyNotFoundException("Attendance record not found.");
            policy.EnsureCanJustify(record, command.Category, command.Reason, command.EvidenceUrl);
            var created = new AttendanceJustification
            {
                AttendanceRecordId = record.Id,
                PreviousStatus = record.Status,
                Category = command.Category.Trim(),
                Reason = command.Reason.Trim(),
                EvidenceUrl = string.IsNullOrWhiteSpace(command.EvidenceUrl) ? null : command.EvidenceUrl.Trim(),
                IsCurrent = true,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                CreatedByUserId = command.ActorUserId
            };
            await repository.SaveJustificationAsync(record, created, transactionCt);
            return created;
        }, ct);
        return AttendanceMapper.MapJustification(justification);
    }
}

public sealed class GetStudentAttendanceSummaryQueryHandler(
    IStudentRepository studentRepository,
    IAttendanceRepository attendanceRepository,
    AttendancePolicy policy)
{
    public async Task<StudentAttendanceSummaryDto> Handle(GetStudentAttendanceSummaryQuery query, CancellationToken ct = default)
    {
        var student = await studentRepository.FindByIdAsync(query.StudentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");
        if (!query.IsAdmin && !await attendanceRepository.CanTeacherViewStudentAsync(
                query.ActorUserId, student.Id, query.CourseId, query.CommissionId, ct))
            throw new ForbiddenException("The teacher cannot view attendance for this student.");
        var records = await attendanceRepository.GetStudentRecordsAsync(
            student.Id, query.CourseId, query.CommissionId,
            query.IsAdmin ? null : query.ActorUserId, ct);
        return AttendanceMapper.MapSummary(student, records, policy);
    }
}

public sealed class GetMyAttendanceSummaryQueryHandler(
    IStudentRepository studentRepository,
    IAttendanceRepository attendanceRepository,
    AttendancePolicy policy)
{
    public async Task<StudentAttendanceSummaryDto> Handle(GetMyAttendanceSummaryQuery query, CancellationToken ct = default)
    {
        var student = await studentRepository.FindByUserIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("Student profile not found.");
        var records = await attendanceRepository.GetStudentRecordsAsync(
            student.Id, query.CourseId, query.CommissionId, null, ct);
        return AttendanceMapper.MapSummary(student, records, policy);
    }
}

public sealed class ExportAttendanceSessionQueryHandler(
    IAttendanceRepository repository,
    IAttendanceReportGenerator generator)
{
    public async Task<AttendanceReportFile> Handle(ExportAttendanceSessionQuery query, CancellationToken ct = default)
    {
        await AttendanceAuthorization.EnsureCanManageSession(repository, query.SessionId, query.ActorUserId, query.IsAdmin, ct);
        var detail = await AttendanceMapper.LoadDetail(repository, query.SessionId, ct);
        var session = detail.Session;
        return await generator.GenerateAsync(new AttendanceReportModel(
            session.Id,
            $"{session.CourseCode} - {session.CourseName}",
            $"{session.CommissionCode} - {session.CommissionName}",
            session.SessionDate,
            session.Scope.ToString(),
            session.Units,
            detail.Records.Select(record => new AttendanceReportRow(
                record.LegajoNumber,
                record.Dni,
                record.StudentName,
                record.Status?.ToString() ?? "NotRecorded",
                record.Notes ?? string.Empty,
                record.Justification is null
                    ? string.Empty
                    : $"{record.Justification.Category}: {record.Justification.Reason}"))
                .ToArray()), query.Format, ct);
    }
}

internal static class AttendanceAuthorization
{
    public static async Task EnsureCanManagePosition(
        IAttendanceRepository repository,
        int positionId,
        DateOnly onDate,
        long actorUserId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (!isAdmin && !await repository.CanTeacherManagePositionAsync(actorUserId, positionId, onDate, ct))
            throw new ForbiddenException("The teacher is not assigned to this course and commission.");
    }

    public static async Task EnsureCanManageSession(
        IAttendanceRepository repository,
        long sessionId,
        long actorUserId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (!isAdmin && !await repository.CanTeacherManageSessionAsync(actorUserId, sessionId, ct))
            throw new ForbiddenException("The teacher cannot manage this attendance session.");
    }
}

internal static class AttendanceMapper
{
    public static AttendanceSessionDto MapSession(AttendanceSession session) => new(
        session.Id,
        session.IdempotencyKey,
        session.TeachingPositionId,
        session.CourseId,
        session.Course.Code,
        session.Course.Name,
        session.CommissionId,
        session.Commission.Code,
        session.Commission.Name,
        session.AcademicYear,
        session.Semester,
        session.SessionDate,
        session.StartTime,
        session.EndTime,
        session.Scope,
        session.Units,
        session.Status,
        session.EditDeadlineUtc,
        session.IsAdministrativelyReopened,
        session.Records.Count,
        session.Reopenings.Count,
        session.CreatedAt,
        session.CreatedByUserId,
        session.ClosedAt,
        session.ClosedByUserId);

    public static async Task<AttendanceSessionDetailDto> LoadDetail(
        IAttendanceRepository repository,
        long sessionId,
        CancellationToken ct)
    {
        var session = await repository.FindSessionAsync(sessionId, ct)
            ?? throw new KeyNotFoundException("Attendance session not found.");
        var roster = await repository.GetRosterAsync(session, ct);
        var records = session.Records.ToDictionary(record => record.EnrollmentId);
        return new AttendanceSessionDetailDto(
            MapSession(session),
            roster.Select(row => records.TryGetValue(row.EnrollmentId, out var record)
                ? MapRecord(row, record)
                : new AttendanceRecordDto(
                    null, row.EnrollmentId, row.StudentId, row.StudentName,
                    row.LegajoNumber, row.Dni, null, null, null, null))
                .ToArray());
    }

    public static AttendanceJustificationDto MapJustification(AttendanceJustification justification) => new(
        justification.Id,
        justification.Category,
        justification.Reason,
        justification.EvidenceUrl,
        justification.CreatedAt,
        justification.CreatedByUserId);

    public static StudentAttendanceSummaryDto MapSummary(
        Student student,
        IReadOnlyList<AttendanceRecord> records,
        AttendancePolicy policy)
    {
        var items = records
            .GroupBy(record => new
            {
                record.AttendanceSession.CourseId,
                record.AttendanceSession.CommissionId,
                record.AttendanceSession.AcademicYear,
                record.AttendanceSession.Semester
            })
            .Select(group =>
            {
                var first = group.First();
                var minimum = group.Select(record => record.Enrollment.StudyPlanCourse?.ApprovalRule?.MinimumAttendancePercentage)
                    .FirstOrDefault(value => value.HasValue);
                var measure = policy.Calculate(group.Select(record => (record.Status, record.AttendanceSession.Units)), minimum);
                return new AttendanceSummaryItemDto(
                    first.AttendanceSession.CourseId,
                    first.AttendanceSession.Course.Code,
                    first.AttendanceSession.Course.Name,
                    first.AttendanceSession.CommissionId,
                    first.AttendanceSession.Commission.Code,
                    first.AttendanceSession.Commission.Name,
                    first.AttendanceSession.AcademicYear,
                    first.AttendanceSession.Semester,
                    minimum,
                    measure.EarnedUnits,
                    measure.PossibleUnits,
                    measure.Percentage,
                    measure.IsAtRisk,
                    group.Count(record => record.Status == AttendanceRecordStatus.Present),
                    group.Count(record => record.Status == AttendanceRecordStatus.Late),
                    group.Count(record => record.Status == AttendanceRecordStatus.Absent),
                    group.Count(record => record.Status == AttendanceRecordStatus.Justified));
            })
            .OrderByDescending(item => item.AcademicYear)
            .ThenByDescending(item => item.Semester)
            .ThenBy(item => item.CourseName)
            .ToArray();
        return new StudentAttendanceSummaryDto(
            student.Id,
            $"{student.User.Username} {student.User.LastName}".Trim(),
            student.LegajoNumber,
            items);
    }

    private static AttendanceRecordDto MapRecord(AttendanceRosterRow row, AttendanceRecord record)
    {
        var currentJustification = record.Justifications.SingleOrDefault(justification => justification.IsCurrent);
        return new AttendanceRecordDto(
            record.Id,
            row.EnrollmentId,
            row.StudentId,
            row.StudentName,
            row.LegajoNumber,
            row.Dni,
            record.Status,
            record.Notes,
            record.UpdatedAt,
            currentJustification is null ? null : MapJustification(currentJustification));
    }
}
