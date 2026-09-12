using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

public sealed class ExamTable
{
    public long Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int AcademicYear { get; set; }
    public int CallNumber { get; set; }
    public DateTime ExamDateUtc { get; set; }
    public DateTime RegistrationDeadlineUtc { get; set; }
    public string Location { get; set; } = string.Empty;
    public ExamTableStatus Status { get; set; } = ExamTableStatus.Open;
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime? GradingStartedAt { get; set; }
    public long? GradingStartedByUserId { get; set; }
    public User? GradingStartedByUser { get; set; }
    public DateTime? PublishedAt { get; set; }
    public long? PublishedByUserId { get; set; }
    public User? PublishedByUser { get; set; }
    public ICollection<ExamTribunalMember> TribunalMembers { get; set; } = [];
    public ICollection<ExamRegistration> Registrations { get; set; } = [];
    public ICollection<ExamTableReopening> Reopenings { get; set; } = [];
}

public sealed class ExamTribunalMember
{
    public long Id { get; set; }
    public long ExamTableId { get; set; }
    public ExamTable ExamTable { get; set; } = null!;
    public long TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public ExamTribunalRole Role { get; set; }
}

public sealed class ExamRegistration
{
    public long Id { get; set; }
    public long ExamTableId { get; set; }
    public ExamTable ExamTable { get; set; } = null!;
    public long EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public DateTime RegisteredAt { get; set; }
    public long RegisteredByUserId { get; set; }
    public User RegisteredByUser { get; set; } = null!;
    public EnrollmentStatus? PreviousEnrollmentStatus { get; set; }
    public decimal? PreviousFinalGrade { get; set; }
    public DateTime? ResultAppliedAt { get; set; }
    public ICollection<ExamGradeRevision> GradeRevisions { get; set; } = [];
}

public sealed class ExamGradeRevision
{
    public long Id { get; set; }
    public long ExamRegistrationId { get; set; }
    public ExamRegistration ExamRegistration { get; set; } = null!;
    public int Version { get; set; }
    public bool IsCurrent { get; set; } = true;
    public ExamResultOutcome Outcome { get; set; }
    public decimal? Grade { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}

public sealed class ExamTableReopening
{
    public long Id { get; set; }
    public long ExamTableId { get; set; }
    public ExamTable ExamTable { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateTime ReopenedAt { get; set; }
    public long ReopenedByUserId { get; set; }
    public User ReopenedByUser { get; set; } = null!;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExamTableStatus
{
    Open = 0,
    Grading = 1,
    Published = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExamTribunalRole
{
    President = 0,
    Vocal = 1
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExamResultOutcome
{
    Passed = 0,
    Failed = 1,
    Absent = 2
}
