using AcademiaDigital.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.Application.Dtos;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record StudentListItemDto(long Id, long UserId, string? Dni, string FullName, string LegajoNumber,
    StudentStatus Status, int CareerId, string CareerName, int? AcademicYear, int? YearNumber, int? CommissionId, string? CommissionName);
public sealed record StudentRecordDto(object Student, object PersonalData, object Address, object EmergencyContact,
    object? CurrentAcademicAssignment, object DocumentsSummary, IReadOnlyList<StudentScholarshipDto> ActiveScholarships,
    IReadOnlyDictionary<string, object?> CustomFields);
public sealed record StatusHistoryDto(long Id, StudentStatus PreviousStatus, StudentStatus NewStatus, string Reason,
    DateTime ChangedAt, long ChangedByUserId);
public sealed record CommissionDto(int Id, int CareerId, string Code, string Name, int AcademicYear, int YearNumber, string Shift, bool IsActive);
public sealed record AcademicAssignmentDto(long Id, long StudentId, int CareerId, int StudyPlanId, int? CommissionId,
    string? CommissionName, int AcademicYear, int YearNumber, DateTime StartedAt, DateTime? EndedAt, bool IsCurrent, string? Reason);
public sealed record DocumentRequirementDto(int Id, string Code, string Name, string? Description, int? CareerId,
    bool IsRequired, bool IsActive, DateOnly? ValidFrom, DateOnly? ValidTo);
public sealed record StudentDocumentDto(long Id, long StudentId, int DocumentRequirementId, string RequirementName,
    string FileUrl, string OriginalFileName, string ContentType, long FileSizeBytes, StudentDocumentStatus Status,
    DateTime SubmittedAt, DateTime? ReviewedAt, string? Observation);
public sealed record ScholarshipDto(int Id, string Code, string Name, string? Description, bool IsActive);
public sealed record StudentScholarshipDto(long Id, long StudentId, int ScholarshipId, string ScholarshipName,
    int AcademicYear, StudentScholarshipStatus Status, DateTime? GrantedAt, DateOnly? ValidFrom, DateOnly? ValidTo, string? Notes);
public sealed record CustomFieldDefinitionDto(int Id, string Key, string Label, CustomFieldDataType DataType,
    bool IsRequired, IReadOnlyList<string>? Options, bool IsActive, int SortOrder);
public sealed record AcademicHistoryItemDto(long EnrollmentId, int AcademicYear, int Semester, int CourseId,
    string CourseCode, string CourseName, string Status, decimal? FinalGrade, DateTime EnrollmentDate);

public sealed class UpdateStudentRequest
{
    [Required, MaxLength(50)] public string LegajoNumber { get; set; } = string.Empty;
    [MaxLength(300)] public string? AddressLine { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? Province { get; set; }
    [MaxLength(20)] public string? PostalCode { get; set; }
    [MaxLength(200)] public string? EmergencyContactName { get; set; }
    [MaxLength(100)] public string? EmergencyContactRelationship { get; set; }
    [MaxLength(30)] public string? EmergencyContactPhone { get; set; }
}
public sealed record ChangeStudentStatusRequest(StudentStatus Status, [Required, MaxLength(500)] string Reason);
public sealed record DeleteStudentRequest([Required, MaxLength(500)] string Reason);
public sealed record UpsertCommissionRequest([Required, MaxLength(30)] string Code,
    [Required, MaxLength(100)] string Name, [Range(2000, 2100)] int AcademicYear,
    [Range(1, 20)] int YearNumber, [Required] string Shift);
public sealed record CreateAcademicAssignmentRequest(int CareerId, int StudyPlanId, int CommissionId,
    [Range(2000, 2100)] int AcademicYear, [Range(1, 20)] int YearNumber, string? Reason);
public sealed record UpsertDocumentRequirementRequest([Required, MaxLength(30)] string Code,
    [Required, MaxLength(150)] string Name, string? Description, int? CareerId, bool IsRequired,
    DateOnly? ValidFrom, DateOnly? ValidTo);
public sealed record CreateStudentDocumentRequest(int DocumentRequirementId, [Required] string FileUrl,
    [Required] string OriginalFileName, [Required] string ContentType,
    [Range(1, long.MaxValue)] long FileSizeBytes);
public sealed record ReviewStudentDocumentRequest(StudentDocumentStatus Status, string? Observation);
public sealed record UpsertScholarshipRequest([Required, MaxLength(30)] string Code,
    [Required, MaxLength(150)] string Name, string? Description);
public sealed record UpsertStudentScholarshipRequest(int ScholarshipId, [Range(2000, 2100)] int AcademicYear,
    StudentScholarshipStatus Status, DateOnly? ValidFrom, DateOnly? ValidTo, string? Notes);
public sealed record UpsertCustomFieldRequest([Required] string Key, [Required] string Label,
    CustomFieldDataType DataType, bool IsRequired, IReadOnlyList<string>? Options, int SortOrder);
public sealed record UpsertCustomValuesRequest(IReadOnlyDictionary<string, object?> Values);
