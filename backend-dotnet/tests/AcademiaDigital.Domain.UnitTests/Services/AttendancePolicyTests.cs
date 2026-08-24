using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AttendancePolicyTests
{
    private readonly AttendancePolicy policy = new();
    private static readonly DateTime Now = new(2027, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Class_hour_session_gets_a_48_hour_deadline()
    {
        var deadline = policy.EnsureCanCreateSession(
            Position(), new DateOnly(2027, 3, 10), new TimeOnly(18, 0), new TimeOnly(20, 0),
            AttendanceScope.ClassHour, 2, Now);

        Assert.Equal(new DateTime(2027, 3, 12, 20, 0, 0, DateTimeKind.Utc), deadline);
    }

    [Fact]
    public void Session_rejects_future_or_incompatible_dates()
    {
        Assert.Throws<ArgumentException>(() => policy.EnsureCanCreateSession(
            Position(), new DateOnly(2027, 3, 11), new TimeOnly(18, 0), new TimeOnly(20, 0),
            AttendanceScope.ClassHour, 2, Now));
        Assert.Throws<ArgumentException>(() => policy.EnsureCanCreateSession(
            Position(), new DateOnly(2026, 3, 10), new TimeOnly(18, 0), new TimeOnly(20, 0),
            AttendanceScope.ClassHour, 2, Now));
    }

    [Fact]
    public void Class_hour_requires_an_ordered_time_range()
        => Assert.Throws<ArgumentException>(() => policy.EnsureCanCreateSession(
            Position(), new DateOnly(2027, 3, 10), new TimeOnly(20, 0), new TimeOnly(18, 0),
            AttendanceScope.ClassHour, 2, Now));

    [Fact]
    public void Full_day_rejects_times_and_multiple_units()
    {
        Assert.Throws<ArgumentException>(() => policy.EnsureCanCreateSession(
            Position(), new DateOnly(2027, 3, 10), new TimeOnly(8, 0), null,
            AttendanceScope.FullDay, 1, Now));
        Assert.Throws<ArgumentException>(() => policy.EnsureCanCreateSession(
            Position(), new DateOnly(2027, 3, 10), null, null,
            AttendanceScope.FullDay, 2, Now));
    }

    [Fact]
    public void Expired_session_requires_an_administrative_reopening()
    {
        var session = Session();
        session.EditDeadlineUtc = Now.AddMinutes(-1);
        Assert.Throws<InvalidOperationException>(() => policy.EnsureEditable(session, Now));
        session.IsAdministrativelyReopened = true;
        policy.EnsureEditable(session, Now);
    }

    [Fact]
    public void Closed_session_is_never_editable()
    {
        var session = Session();
        session.Status = AttendanceSessionStatus.Closed;
        session.IsAdministrativelyReopened = true;
        Assert.Throws<InvalidOperationException>(() => policy.EnsureEditable(session, Now));
    }

    [Fact]
    public void Bulk_load_cannot_set_justified_directly()
        => Assert.Throws<ArgumentException>(() =>
            policy.EnsureRecordStatusCanBeLoaded(AttendanceRecordStatus.Justified));

    [Fact]
    public void Justification_requires_an_absence_or_late_arrival_and_safe_evidence()
    {
        var absent = new AttendanceRecord { Status = AttendanceRecordStatus.Absent };
        policy.EnsureCanJustify(absent, "Medical", "Valid certificate", "storage://attendance/evidence.pdf");
        absent.Status = AttendanceRecordStatus.Present;
        Assert.Throws<InvalidOperationException>(() =>
            policy.EnsureCanJustify(absent, "Medical", "Valid certificate", null));
        absent.Status = AttendanceRecordStatus.Absent;
        Assert.Throws<ArgumentException>(() =>
            policy.EnsureCanJustify(absent, "Medical", "Valid certificate", "file:///tmp/private.pdf"));
    }

    [Fact]
    public void Calculation_weights_present_late_absent_and_excludes_justified()
    {
        var result = policy.Calculate(new[]
        {
            (AttendanceRecordStatus.Present, 2),
            (AttendanceRecordStatus.Late, 2),
            (AttendanceRecordStatus.Absent, 2),
            (AttendanceRecordStatus.Justified, 4)
        }, 75m);

        Assert.Equal(3m, result.EarnedUnits);
        Assert.Equal(6m, result.PossibleUnits);
        Assert.Equal(50m, result.Percentage);
        Assert.True(result.IsAtRisk);
    }

    [Fact]
    public void Calculation_without_denominator_has_no_percentage_or_risk()
    {
        var result = policy.Calculate(new[] { (AttendanceRecordStatus.Justified, 2) }, 75m);
        Assert.Null(result.Percentage);
        Assert.False(result.IsAtRisk);
    }

    private static TeachingPosition Position() => new()
    {
        Id = 5,
        IsActive = true,
        CommissionId = 3,
        AcademicYear = 2027
    };

    private static AttendanceSession Session() => new()
    {
        Status = AttendanceSessionStatus.Open,
        EditDeadlineUtc = Now.AddHours(1)
    };
}
