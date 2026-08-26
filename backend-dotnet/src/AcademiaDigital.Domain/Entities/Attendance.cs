using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

public sealed class AttendanceSession
{
    public long Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int TeachingPositionId { get; set; }
    public TeachingPosition TeachingPosition { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int CommissionId { get; set; }
    public Commission Commission { get; set; } = null!;
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public DateOnly SessionDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public AttendanceScope Scope { get; set; }
    public int Units { get; set; } = 1;
    public AttendanceSessionStatus Status { get; set; } = AttendanceSessionStatus.Open;
    public DateTime EditDeadlineUtc { get; set; }
    public bool IsAdministrativelyReopened { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime? ClosedAt { get; set; }
    public long? ClosedByUserId { get; set; }
    public User? ClosedByUser { get; set; }
    public ICollection<AttendanceRecord> Records { get; set; } = [];
    public ICollection<AttendanceSessionReopening> Reopenings { get; set; } = [];
}

public sealed class AttendanceRecord
{
    public long Id { get; set; }
    public long AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;
    public long EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public AttendanceRecordStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;
    public ICollection<AttendanceJustification> Justifications { get; set; } = [];
}

public sealed class AttendanceJustification
{
    public long Id { get; set; }
    public long AttendanceRecordId { get; set; }
    public AttendanceRecord AttendanceRecord { get; set; } = null!;
    public AttendanceRecordStatus PreviousStatus { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}

public sealed class AttendanceSessionReopening
{
    public long Id { get; set; }
    public long AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateTime ReopenedAt { get; set; }
    public long ReopenedByUserId { get; set; }
    public User ReopenedByUser { get; set; } = null!;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceScope
{
    ClassHour = 0,
    FullDay = 1
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceSessionStatus
{
    Open = 0,
    Closed = 1
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttendanceRecordStatus
{
    Present = 0,
    Late = 1,
    Absent = 2,
    Justified = 3
}
