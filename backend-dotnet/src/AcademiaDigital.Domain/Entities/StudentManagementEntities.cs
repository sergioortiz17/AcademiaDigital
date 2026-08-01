using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

public class StudentStatusHistory
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public StudentStatus PreviousStatus { get; set; }
    public StudentStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public long ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;
}

public class Commission
{
    public int Id { get; set; }
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public int YearNumber { get; set; }
    public string Shift { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StudentAcademicAssignment
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public long StudentCareerId { get; set; }
    public StudentCareer StudentCareer { get; set; } = null!;
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public int StudyPlanId { get; set; }
    public StudyPlan StudyPlan { get; set; } = null!;
    public int? CommissionId { get; set; }
    public Commission? Commission { get; set; }
    public int AcademicYear { get; set; }
    public int YearNumber { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string? Reason { get; set; }
    public long AssignedByUserId { get; set; }
    public User AssignedByUser { get; set; } = null!;
}

public class DocumentRequirement
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CareerId { get; set; }
    public Career? Career { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudentDocumentStatus { Submitted, Approved, Rejected, Expired }

public class StudentDocument
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int DocumentRequirementId { get; set; }
    public DocumentRequirement DocumentRequirement { get; set; } = null!;
    public string FileUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public StudentDocumentStatus Status { get; set; } = StudentDocumentStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public string? Observation { get; set; }
}

public class Scholarship
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudentScholarshipStatus { Requested, Granted, Rejected, Revoked, Expired }

public class StudentScholarship
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int ScholarshipId { get; set; }
    public Scholarship Scholarship { get; set; } = null!;
    public int AcademicYear { get; set; }
    public StudentScholarshipStatus Status { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? Notes { get; set; }
    public long UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CustomFieldDataType { Text, Number, Date, Boolean, Select }

public class CustomFieldDefinition
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public CustomFieldDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public string? OptionsJson { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class StudentCustomFieldValue
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CustomFieldDefinitionId { get; set; }
    public CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;
    public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long UpdatedByUserId { get; set; }
    public User UpdatedByUser { get; set; } = null!;
}
