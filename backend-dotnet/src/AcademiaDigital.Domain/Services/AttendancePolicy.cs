using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed record AttendanceMeasure(
    decimal EarnedUnits,
    decimal PossibleUnits,
    decimal? Percentage,
    bool IsAtRisk);

public sealed class AttendancePolicy
{
    public static readonly TimeSpan DefaultEditWindow = TimeSpan.FromHours(48);

    public DateTime EnsureCanCreateSession(
        TeachingPosition position,
        DateOnly sessionDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        AttendanceScope scope,
        int units,
        DateTime nowUtc)
    {
        if (!position.IsActive || position.CommissionId is null)
            throw new InvalidOperationException("Attendance requires an active teaching position with a commission.");
        if (sessionDate.Year != position.AcademicYear)
            throw new ArgumentException("Session date must belong to the teaching position academic year.");
        if (sessionDate > DateOnly.FromDateTime(nowUtc))
            throw new ArgumentException("Attendance sessions cannot be created in the future.");
        if (units is < 1 or > 12)
            throw new ArgumentException("Attendance units must be between 1 and 12.");

        if (scope == AttendanceScope.ClassHour)
        {
            if (!startTime.HasValue || !endTime.HasValue || endTime <= startTime)
                throw new ArgumentException("Class-hour attendance requires a valid start and end time.");
        }
        else if (startTime.HasValue || endTime.HasValue || units != 1)
        {
            throw new ArgumentException("Full-day attendance does not accept times and always uses one unit.");
        }

        var occurrenceEnd = sessionDate.ToDateTime(
            scope == AttendanceScope.ClassHour ? endTime!.Value : TimeOnly.MaxValue,
            DateTimeKind.Utc);
        return occurrenceEnd.Add(DefaultEditWindow);
    }

    public void EnsureEditable(AttendanceSession session, DateTime nowUtc)
    {
        if (session.Status != AttendanceSessionStatus.Open)
            throw new InvalidOperationException("The attendance session is closed.");
        if (!session.IsAdministrativelyReopened && nowUtc > session.EditDeadlineUtc)
            throw new InvalidOperationException("The 48-hour attendance edit window has expired.");
    }

    public void EnsureCanClose(AttendanceSession session)
    {
        if (session.Status != AttendanceSessionStatus.Open)
            throw new InvalidOperationException("The attendance session is already closed.");
    }

    public void EnsureCanReopen(AttendanceSession session, string reason)
    {
        if (session.Status != AttendanceSessionStatus.Closed)
            throw new InvalidOperationException("Only a closed attendance session can be reopened.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new ArgumentException("A reopening reason of at least three characters is required.");
    }

    public void EnsureRecordStatusCanBeLoaded(AttendanceRecordStatus status)
    {
        if (!Enum.IsDefined(status) || status == AttendanceRecordStatus.Justified)
            throw new ArgumentException("Bulk attendance accepts Present, Late or Absent; Justified requires an audited justification.");
    }

    public void EnsureCanJustify(AttendanceRecord record, string category, string reason, string? evidenceUrl)
    {
        if (record.Status is not AttendanceRecordStatus.Absent and not AttendanceRecordStatus.Late)
            throw new InvalidOperationException("Only an absence or late arrival can be justified.");
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Justification category is required.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new ArgumentException("Justification reason must contain at least three characters.");
        if (!string.IsNullOrWhiteSpace(evidenceUrl)
            && !Uri.TryCreate(evidenceUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Evidence URL must be absolute.");
        if (!string.IsNullOrWhiteSpace(evidenceUrl)
            && !evidenceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !evidenceUrl.StartsWith("storage://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Evidence URL must use HTTPS or a storage key.");
    }

    public AttendanceMeasure Calculate(
        IEnumerable<(AttendanceRecordStatus Status, int Units)> records,
        decimal? minimumAttendancePercentage)
    {
        decimal earned = 0;
        decimal possible = 0;
        foreach (var (status, units) in records)
        {
            if (status == AttendanceRecordStatus.Justified) continue;
            possible += units;
            earned += status switch
            {
                AttendanceRecordStatus.Present => units,
                AttendanceRecordStatus.Late => units * 0.5m,
                _ => 0m
            };
        }

        decimal? percentage = possible == 0 ? null : Math.Round(earned / possible * 100m, 2);
        return new AttendanceMeasure(
            earned,
            possible,
            percentage,
            percentage < minimumAttendancePercentage);
    }
}
