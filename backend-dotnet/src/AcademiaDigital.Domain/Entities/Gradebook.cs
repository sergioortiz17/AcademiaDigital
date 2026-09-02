using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

public sealed class Gradebook
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
    public GradebookStatus Status { get; set; } = GradebookStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public long? SubmittedByUserId { get; set; }
    public User? SubmittedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? PublishedAt { get; set; }
    public long? PublishedByUserId { get; set; }
    public User? PublishedByUser { get; set; }
    public DateTime? ClosedAt { get; set; }
    public long? ClosedByUserId { get; set; }
    public User? ClosedByUser { get; set; }
    public ICollection<GradebookEvaluation> Evaluations { get; set; } = [];
    public ICollection<GradeEntryRevision> GradeRevisions { get; set; } = [];
    public ICollection<GradebookReopening> Reopenings { get; set; } = [];
}

public sealed class GradebookEvaluation
{
    public long Id { get; set; }
    public long GradebookId { get; set; }
    public Gradebook Gradebook { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal WeightPercentage { get; set; }
    public decimal MaximumScore { get; set; } = 10m;
    public int DisplayOrder { get; set; }
    public ICollection<GradeEntryRevision> GradeRevisions { get; set; } = [];
}

public sealed class GradeEntryRevision
{
    public long Id { get; set; }
    public long GradebookId { get; set; }
    public Gradebook Gradebook { get; set; } = null!;
    public long EvaluationId { get; set; }
    public GradebookEvaluation Evaluation { get; set; } = null!;
    public long EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int Version { get; set; }
    public bool IsCurrent { get; set; } = true;
    public decimal Score { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}

public sealed class GradebookReopening
{
    public long Id { get; set; }
    public long GradebookId { get; set; }
    public Gradebook Gradebook { get; set; } = null!;
    public GradebookStatus PreviousStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ReopenedAt { get; set; }
    public long ReopenedByUserId { get; set; }
    public User ReopenedByUser { get; set; } = null!;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GradebookStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Published = 3,
    Closed = 4
}
