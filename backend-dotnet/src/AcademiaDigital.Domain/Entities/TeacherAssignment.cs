namespace AcademiaDigital.Domain.Entities;

public sealed class TeacherAssignment
{
    public long Id { get; set; }
    public int TeachingPositionId { get; set; }
    public TeachingPosition TeachingPosition { get; set; } = null!;
    public long TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public DateOnly StartedOn { get; set; }
    public DateOnly? EndedOn { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string? AssignmentReason { get; set; }
    public string? EndReason { get; set; }
    public long? AssignedByUserId { get; set; }
    public User? AssignedByUser { get; set; }
    public long? EndedByUserId { get; set; }
    public User? EndedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
