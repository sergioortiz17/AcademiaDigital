using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Attendance;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Attendance;

public sealed class AttendanceHandlersTests
{
    private static readonly DateTimeOffset Now = new(2027, 3, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_session_is_idempotent_and_uses_the_position_snapshot()
    {
        var positions = Substitute.For<ITeachingPositionRepository>();
        var attendance = Substitute.For<IAttendanceRepository>();
        positions.FindByIdAsync(5, Arg.Any<CancellationToken>()).Returns(Position());
        attendance.CreateIdempotentAsync(Arg.Any<AttendanceSession>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var session = call.Arg<AttendanceSession>();
                session.Id = 10;
                session.Course = Course();
                session.Commission = Commission();
                return (session, true);
            });
        var handler = new CreateAttendanceSessionCommandHandler(
            positions, attendance, new AttendancePolicy(), new ImmediateUnitOfWork(),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateAttendanceSessionCommand(
            "attendance-request-001", 5, new DateOnly(2027, 3, 10),
            new TimeOnly(8, 0), new TimeOnly(10, 0), AttendanceScope.ClassHour, 2, 99, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(10, result.Id);
        Assert.Equal(2, result.CourseId);
        Assert.Equal(new DateTime(2027, 3, 12, 10, 0, 0, DateTimeKind.Utc), result.EditDeadlineUtc);
        await attendance.Received(1).CreateIdempotentAsync(
            Arg.Is<AttendanceSession>(session => session.IdempotencyKey == "attendance-request-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Professor_cannot_create_a_session_outside_their_assignment()
    {
        var positions = Substitute.For<ITeachingPositionRepository>();
        var attendance = Substitute.For<IAttendanceRepository>();
        positions.FindByIdAsync(5, Arg.Any<CancellationToken>()).Returns(Position());
        attendance.CanTeacherManagePositionAsync(88, 5, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateAttendanceSessionCommandHandler(
            positions, attendance, new AttendancePolicy(), new ImmediateUnitOfWork(),
            new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new CreateAttendanceSessionCommand(
            "attendance-request-002", 5, new DateOnly(2027, 3, 10),
            new TimeOnly(8, 0), new TimeOnly(10, 0), AttendanceScope.ClassHour, 2, 88, false),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bulk_load_rejects_an_enrollment_outside_the_session_roster()
    {
        var attendance = Substitute.For<IAttendanceRepository>();
        var session = Session();
        attendance.FindSessionForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        attendance.GetRosterAsync(session, Arg.Any<CancellationToken>()).Returns(new[] { Roster() });
        var handler = new SaveAttendanceRecordsCommandHandler(
            attendance, new AttendancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new SaveAttendanceRecordsCommand(
            10, new[] { new AttendanceRecordInput(999, AttendanceRecordStatus.Present, null) }, 99, true),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bulk_load_maps_roster_student_and_saves_in_one_transaction()
    {
        var attendance = Substitute.For<IAttendanceRepository>();
        var session = Session();
        attendance.FindSessionForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        attendance.GetRosterAsync(session, Arg.Any<CancellationToken>()).Returns(new[] { Roster() });
        attendance.FindSessionAsync(10, Arg.Any<CancellationToken>()).Returns(call =>
        {
            session.Records =
            [
                new AttendanceRecord
                {
                    Id = 20, EnrollmentId = 30, StudentId = 40,
                    Student = Student(), Enrollment = Enrollment(),
                    Status = AttendanceRecordStatus.Late, UpdatedAt = Now.UtcDateTime
                }
            ];
            return session;
        });
        var handler = new SaveAttendanceRecordsCommandHandler(
            attendance, new AttendancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new SaveAttendanceRecordsCommand(
            10, new[] { new AttendanceRecordInput(30, AttendanceRecordStatus.Late, "Traffic") }, 99, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(AttendanceRecordStatus.Late, Assert.Single(result.Records).Status);
        await attendance.Received(1).SaveRecordsAsync(session,
            Arg.Is<IReadOnlyList<AttendanceRecord>>(records => records.Single().StudentId == 40),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Closing_and_reopening_preserve_an_audited_reopening()
    {
        var attendance = Substitute.For<IAttendanceRepository>();
        var session = Session();
        attendance.FindSessionForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        attendance.FindSessionAsync(10, Arg.Any<CancellationToken>()).Returns(session);
        var unitOfWork = new ImmediateUnitOfWork();
        var time = new FixedTimeProvider(Now);
        var close = new CloseAttendanceSessionCommandHandler(attendance, new AttendancePolicy(), unitOfWork, time);
        var reopen = new ReopenAttendanceSessionCommandHandler(attendance, new AttendancePolicy(), unitOfWork, time);

        var closed = await close.Handle(
            new CloseAttendanceSessionCommand(10, 99, true), TestContext.Current.CancellationToken);
        var reopened = await reopen.Handle(
            new ReopenAttendanceSessionCommand(10, "Correction authorized", 99), TestContext.Current.CancellationToken);

        Assert.Equal(AttendanceSessionStatus.Closed, closed.Status);
        Assert.Equal(AttendanceSessionStatus.Open, reopened.Status);
        Assert.True(reopened.IsAdministrativelyReopened);
        Assert.Equal(1, reopened.ReopeningCount);
        Assert.Equal("Correction authorized", Assert.Single(session.Reopenings).Reason);
    }

    [Fact]
    public async Task Justification_is_append_only_and_keeps_the_previous_status()
    {
        var attendance = Substitute.For<IAttendanceRepository>();
        var record = new AttendanceRecord { Id = 20, Status = AttendanceRecordStatus.Absent };
        attendance.FindRecordForUpdateAsync(20, Arg.Any<CancellationToken>()).Returns(record);
        var handler = new JustifyAttendanceRecordCommandHandler(
            attendance, new AttendancePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new JustifyAttendanceRecordCommand(
            20, "Medical", "Approved certificate", "https://files.example/evidence.pdf", 99),
            TestContext.Current.CancellationToken);

        Assert.Equal("Medical", result.Category);
        await attendance.Received(1).SaveJustificationAsync(record,
            Arg.Is<AttendanceJustification>(justification =>
                justification.PreviousStatus == AttendanceRecordStatus.Absent && justification.IsCurrent),
            Arg.Any<CancellationToken>());
    }

    private static Course Course() => new() { Id = 2, Code = "MAT", Name = "Mathematics" };
    private static Commission Commission() => new() { Id = 3, Code = "C1", Name = "Commission 1" };
    private static Student Student() => new()
    {
        Id = 40,
        LegajoNumber = "LEG-40",
        User = new User { Username = "Ada", LastName = "Lovelace", Dni = "12345678" }
    };
    private static Enrollment Enrollment() => new() { Id = 30, StudentId = 40 };
    private static TeachingPosition Position() => new()
    {
        Id = 5,
        CourseId = 2,
        Course = Course(),
        CommissionId = 3,
        Commission = Commission(),
        AcademicYear = 2027,
        Semester = 1,
        IsActive = true
    };
    private static AttendanceSession Session() => new()
    {
        Id = 10,
        IdempotencyKey = "attendance-request-001",
        TeachingPositionId = 5,
        CourseId = 2,
        Course = Course(),
        CommissionId = 3,
        Commission = Commission(),
        AcademicYear = 2027,
        Semester = 1,
        SessionDate = new DateOnly(2027, 3, 10),
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(10, 0),
        Scope = AttendanceScope.ClassHour,
        Units = 2,
        Status = AttendanceSessionStatus.Open,
        EditDeadlineUtc = Now.UtcDateTime.AddHours(4),
        Reopenings = [],
        Records = []
    };
    private static AttendanceRosterRow Roster() => new(30, 40, "Ada Lovelace", "LEG-40", "12345678");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
            => operation(ct);
        public Task<T> ExecuteInSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
            => operation(ct);
    }
}
