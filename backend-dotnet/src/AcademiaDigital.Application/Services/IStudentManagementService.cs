using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Application.Services;

public interface IStudentManagementService
{
    Task<PagedResult<StudentListItemDto>> SearchStudentsAsync(string? search, int? careerId, StudentStatus? status,
        int? academicYear, int? commissionId, int page, int pageSize, CancellationToken ct);
    Task<StudentListItemDto> UpdateStudentAsync(long id, UpdateStudentRequest request, CancellationToken ct);
    Task<StatusHistoryDto> ChangeStatusAsync(long id, StudentStatus status, string reason, long actorId, CancellationToken ct);
    Task<IReadOnlyList<StatusHistoryDto>> GetStatusHistoryAsync(long id, CancellationToken ct);
    Task SoftDeleteStudentAsync(long id, string reason, long actorId, CancellationToken ct);
    Task<StudentRecordDto> GetRecordAsync(long id, CancellationToken ct);
    Task<bool> IsOwnerAsync(long studentId, long userId, CancellationToken ct);
    Task<IReadOnlyList<StudentCareerDto>> GetStudentCareersAsync(long studentId, CancellationToken ct);
    Task<StudentCareerDto> AddStudentCareerAsync(long studentId, AddStudentCareerRequest request, CancellationToken ct);

    Task<IReadOnlyList<CommissionDto>> GetCommissionsAsync(int careerId, int? academicYear, CancellationToken ct);
    Task<CommissionDto> SaveCommissionAsync(int careerId, int? id, UpsertCommissionRequest request, CancellationToken ct);
    Task DisableCommissionAsync(int careerId, int id, CancellationToken ct);
    Task<AcademicAssignmentDto> AssignAcademicAsync(long studentId, CreateAcademicAssignmentRequest request, long actorId, CancellationToken ct);
    Task<IReadOnlyList<AcademicAssignmentDto>> GetAssignmentsAsync(long studentId, int? academicYear, CancellationToken ct);

    Task<IReadOnlyList<DocumentRequirementDto>> GetRequirementsAsync(int? careerId, CancellationToken ct);
    Task<DocumentRequirementDto> SaveRequirementAsync(int? id, UpsertDocumentRequirementRequest request, CancellationToken ct);
    Task DisableRequirementAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<StudentDocumentDto>> GetDocumentsAsync(long studentId, CancellationToken ct);
    Task<StudentDocumentDto> AddDocumentAsync(long studentId, CreateStudentDocumentRequest request, CancellationToken ct);
    Task<StudentDocumentDto> ReviewDocumentAsync(long studentId, long documentId, ReviewStudentDocumentRequest request, long actorId, CancellationToken ct);
    Task DeleteDocumentAsync(long studentId, long documentId, CancellationToken ct);
    Task<IReadOnlyList<DocumentRequirementDto>> GetPendingDocumentsAsync(long studentId, CancellationToken ct);

    Task<IReadOnlyList<ScholarshipDto>> GetScholarshipsAsync(CancellationToken ct);
    Task<ScholarshipDto> SaveScholarshipAsync(int? id, UpsertScholarshipRequest request, CancellationToken ct);
    Task DisableScholarshipAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<StudentScholarshipDto>> GetStudentScholarshipsAsync(long studentId, CancellationToken ct);
    Task<StudentScholarshipDto> SaveStudentScholarshipAsync(long studentId, long? id, UpsertStudentScholarshipRequest request, long actorId, CancellationToken ct);
    Task RevokeStudentScholarshipAsync(long studentId, long id, long actorId, CancellationToken ct);

    Task<IReadOnlyList<CustomFieldDefinitionDto>> GetCustomFieldsAsync(CancellationToken ct);
    Task<CustomFieldDefinitionDto> SaveCustomFieldAsync(int? id, UpsertCustomFieldRequest request, CancellationToken ct);
    Task DisableCustomFieldAsync(int id, CancellationToken ct);
    Task<IReadOnlyDictionary<string, object?>> GetCustomValuesAsync(long studentId, CancellationToken ct);
    Task<IReadOnlyDictionary<string, object?>> SaveCustomValuesAsync(long studentId, UpsertCustomValuesRequest request, long actorId, CancellationToken ct);
    Task<PagedResult<AcademicHistoryItemDto>> GetAcademicHistoryAsync(long studentId, int? academicYear, int page, int pageSize, CancellationToken ct);
}
