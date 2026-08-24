using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionDocumentPolicy
{
    public void EnsureRequirementApplies(DocumentRequirement requirement, int careerId, DateOnly today)
    {
        if (!requirement.IsActive
            || (requirement.CareerId.HasValue && requirement.CareerId != careerId)
            || (requirement.ValidFrom.HasValue && requirement.ValidFrom > today)
            || (requirement.ValidTo.HasValue && requirement.ValidTo < today))
            throw new InvalidOperationException("Document requirement does not apply to this admission application.");
    }

    public void EnsureCanSubmit(AdmissionApplicationStatus status)
    {
        if (status is AdmissionApplicationStatus.Confirmed
            or AdmissionApplicationStatus.Expired
            or AdmissionApplicationStatus.Rejected)
            throw new InvalidOperationException("Documents cannot be submitted for an application in its current status.");
    }

    public void EnsureCanReview(
        StudentDocumentStatus currentStatus,
        StudentDocumentStatus targetStatus,
        string? observation)
    {
        if (currentStatus != StudentDocumentStatus.Submitted)
            throw new InvalidOperationException("Only submitted documents can be reviewed.");
        if (targetStatus is not (StudentDocumentStatus.Approved or StudentDocumentStatus.Rejected))
            throw new ArgumentException("Review status must be Approved or Rejected.");
        if (targetStatus == StudentDocumentStatus.Rejected && string.IsNullOrWhiteSpace(observation))
            throw new ArgumentException("Observation is required when rejecting a document.");
    }

    public void EnsureRequiredDocumentsApproved(
        IReadOnlyCollection<DocumentRequirement> requiredDocuments,
        IReadOnlyCollection<AdmissionApplicationDocument> submittedDocuments)
    {
        var approvedRequirementIds = submittedDocuments
            .Where(document => document.Status == StudentDocumentStatus.Approved)
            .Select(document => document.DocumentRequirementId)
            .ToHashSet();
        var missingCodes = requiredDocuments
            .Where(requirement => requirement.IsRequired && !approvedRequirementIds.Contains(requirement.Id))
            .OrderBy(requirement => requirement.Code)
            .Select(requirement => requirement.Code)
            .ToArray();
        if (missingCodes.Length > 0)
            throw new InvalidOperationException(
                $"Required admission documents are not approved: {string.Join(", ", missingCodes)}.");
    }
}
