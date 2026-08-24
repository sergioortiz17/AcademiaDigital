using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IAdmissionRepository
{
    Task<AdmissionForm?> FindActiveFormBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionForm>> GetFormsAsync(CancellationToken ct = default);
    Task<AdmissionForm?> FindFormByIdAsync(int id, CancellationToken ct = default);
    Task<AdmissionForm?> LockFormForCapacityAsync(int id, CancellationToken ct = default);
    Task<bool> FormSlugExistsAsync(string slug, CancellationToken ct = default);
    Task<bool> CommissionTargetExistsAsync(int commissionId, CancellationToken ct = default);
    Task<AdmissionForm> CreateFormAsync(AdmissionForm form, CancellationToken ct = default);
    Task<AdmissionForm> UpdateFormAsync(AdmissionForm form, CancellationToken ct = default);
    Task<bool> ApplicationExistsAsync(
        int admissionFormId,
        string applicantEmail,
        string applicantDni,
        CancellationToken ct = default);
    Task<AdmissionApplication> CreateApplicationAsync(
        AdmissionApplication application,
        CancellationToken ct = default);
    Task<int> CountCapacityOccupyingApplicationsAsync(int admissionFormId, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionApplication>> GetExpiredReservationsAsync(
        int admissionFormId,
        DateTime expiresAtOrBefore,
        CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionApplication>> GetWaitlistedApplicationsAsync(
        int admissionFormId,
        int limit,
        long? excludedApplicationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetFormIdsWithExpiredReservationsAsync(
        DateTime expiresAtOrBefore,
        CancellationToken ct = default);
    Task<bool> HasEarlierWaitlistedApplicationAsync(
        int admissionFormId,
        DateTime createdAt,
        long applicationId,
        CancellationToken ct = default);
    Task<(IReadOnlyList<AdmissionApplication> Items, int Total)> GetApplicationsAsync(
        int? admissionFormId,
        AdmissionApplicationStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<AdmissionApplication?> FindApplicationByPublicIdAsync(
        Guid publicId,
        bool trackChanges,
        CancellationToken ct = default);
    Task<AdmissionApplication> UpdateApplicationStatusAsync(
        AdmissionApplication application,
        AdmissionApplicationStatusHistory history,
        CancellationToken ct = default);
    Task<DocumentRequirement?> FindDocumentRequirementAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentRequirement>> GetApplicableRequiredDocumentRequirementsAsync(
        int careerId,
        DateOnly date,
        CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionApplicationDocument>> GetApplicationDocumentsAsync(
        long admissionApplicationId,
        bool trackChanges,
        CancellationToken ct = default);
    Task<AdmissionApplicationDocument?> FindApplicationDocumentAsync(
        Guid applicationPublicId,
        long documentId,
        bool trackChanges,
        CancellationToken ct = default);
    Task<AdmissionApplicationDocument> CreateApplicationDocumentAsync(
        AdmissionApplicationDocument document,
        CancellationToken ct = default);
    Task<AdmissionApplicationDocument> UpdateApplicationDocumentAsync(
        AdmissionApplicationDocument document,
        CancellationToken ct = default);
    Task<AdmissionAgreement> CreateAgreementWithOutboxAsync(
        AdmissionAgreement agreement,
        OutboxMessage message,
        CancellationToken ct = default);
    Task<AdmissionAgreement?> FindAgreementByApplicationPublicIdAsync(
        Guid publicId,
        bool trackChanges,
        CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetClaimableOutboxMessagesAsync(
        DateTime availableAtOrBefore,
        DateTime processingStartedBefore,
        int limit,
        CancellationToken ct = default);
}
