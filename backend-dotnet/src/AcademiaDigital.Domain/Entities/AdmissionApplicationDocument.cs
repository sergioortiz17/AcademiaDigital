namespace AcademiaDigital.Domain.Entities;

public sealed class AdmissionApplicationDocument
{
    public long Id { get; set; }
    public long AdmissionApplicationId { get; set; }
    public int DocumentRequirementId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public StudentDocumentStatus Status { get; set; } = StudentDocumentStatus.Submitted;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? Observation { get; set; }

    public AdmissionApplication AdmissionApplication { get; set; } = null!;
    public DocumentRequirement DocumentRequirement { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
