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
            throw new InvalidOperationException("El requisito de documento no aplica a esta solicitud de admisión.");
    }

    public void EnsureCanSubmit(AdmissionApplicationStatus status)
    {
        if (status is AdmissionApplicationStatus.Confirmed
            or AdmissionApplicationStatus.Expired
            or AdmissionApplicationStatus.Rejected)
            throw new InvalidOperationException("No se pueden enviar documentos para una solicitud en su estado actual.");
    }

    public void EnsureCanReview(
        StudentDocumentStatus currentStatus,
        StudentDocumentStatus targetStatus,
        string? observation)
    {
        if (currentStatus != StudentDocumentStatus.Submitted)
            throw new InvalidOperationException("Solo se pueden revisar los documentos enviados.");
        if (targetStatus is not (StudentDocumentStatus.Approved or StudentDocumentStatus.Rejected))
            throw new ArgumentException("El estado de revisión debe ser Aprobado o Rechazado.");
        if (targetStatus == StudentDocumentStatus.Rejected && string.IsNullOrWhiteSpace(observation))
            throw new ArgumentException("La observación es obligatoria al rechazar un documento.");
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
                $"No están aprobados los documentos de admisión obligatorios: {string.Join(", ", missingCodes)}.");
    }
}
