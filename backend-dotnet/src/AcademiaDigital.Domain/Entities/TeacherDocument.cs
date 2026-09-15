namespace AcademiaDigital.Domain.Entities;

public sealed class TeacherDocument
{
    public long Id { get; set; }
    public long TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public StudentDocumentStatus Status { get; set; } = StudentDocumentStatus.Submitted;
    public DateTime SubmittedAt { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public string? Observation { get; set; }
}
