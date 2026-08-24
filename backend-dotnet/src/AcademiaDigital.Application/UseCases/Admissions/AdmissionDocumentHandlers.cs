using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Admissions;

public sealed record AdmissionApplicationDocumentDto(
    long Id,
    Guid ApplicationPublicId,
    int DocumentRequirementId,
    string RequirementCode,
    string RequirementName,
    string FileUrl,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    long? ReviewedByUserId,
    string? Observation);

public sealed record GetAdmissionApplicationDocumentsQuery(Guid PublicId);

public sealed class GetAdmissionApplicationDocumentsQueryHandler(IAdmissionRepository repository)
{
    public async Task<IReadOnlyList<AdmissionApplicationDocumentDto>> Handle(
        GetAdmissionApplicationDocumentsQuery query,
        CancellationToken ct = default)
    {
        var application = await repository.FindApplicationByPublicIdAsync(query.PublicId, false, ct)
            ?? throw new KeyNotFoundException("Admission application not found.");
        return (await repository.GetApplicationDocumentsAsync(application.Id, false, ct))
            .Select(document => Map(query.PublicId, document))
            .ToArray();
    }

    internal static AdmissionApplicationDocumentDto Map(
        Guid applicationPublicId,
        AdmissionApplicationDocument document)
        => new(
            document.Id,
            applicationPublicId,
            document.DocumentRequirementId,
            document.DocumentRequirement.Code,
            document.DocumentRequirement.Name,
            document.FileUrl,
            document.OriginalFileName,
            document.ContentType,
            document.FileSizeBytes,
            document.Status.ToString(),
            document.SubmittedAt,
            document.ReviewedAt,
            document.ReviewedByUserId,
            document.Observation);
}

public sealed record SubmitAdmissionApplicationDocumentCommand(
    Guid PublicId,
    int DocumentRequirementId,
    string FileUrl,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes);

public sealed class SubmitAdmissionApplicationDocumentCommandHandler(
    IAdmissionRepository repository,
    AdmissionDocumentPolicy policy,
    TimeProvider timeProvider)
{
    public async Task<AdmissionApplicationDocumentDto> Handle(
        SubmitAdmissionApplicationDocumentCommand command,
        CancellationToken ct = default)
    {
        var application = await repository.FindApplicationByPublicIdAsync(command.PublicId, false, ct)
            ?? throw new KeyNotFoundException("Admission application not found.");
        var requirement = await repository.FindDocumentRequirementAsync(command.DocumentRequirementId, ct)
            ?? throw new KeyNotFoundException("Document requirement not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        policy.EnsureCanSubmit(application.Status);
        policy.EnsureRequirementApplies(
            requirement,
            application.AdmissionForm.CareerId,
            DateOnly.FromDateTime(now));

        var document = new AdmissionApplicationDocument
        {
            AdmissionApplicationId = application.Id,
            DocumentRequirementId = requirement.Id,
            FileUrl = command.FileUrl.Trim(),
            OriginalFileName = command.OriginalFileName.Trim(),
            ContentType = command.ContentType.Trim(),
            FileSizeBytes = command.FileSizeBytes,
            Status = StudentDocumentStatus.Submitted,
            SubmittedAt = now
        };
        var created = await repository.CreateApplicationDocumentAsync(document, ct);
        created.DocumentRequirement = requirement;
        return GetAdmissionApplicationDocumentsQueryHandler.Map(command.PublicId, created);
    }
}

public sealed record ReviewAdmissionApplicationDocumentCommand(
    Guid PublicId,
    long DocumentId,
    StudentDocumentStatus Status,
    string? Observation,
    long ReviewedByUserId);

public sealed class ReviewAdmissionApplicationDocumentCommandHandler(
    IAdmissionRepository repository,
    AdmissionDocumentPolicy policy,
    TimeProvider timeProvider)
{
    public async Task<AdmissionApplicationDocumentDto> Handle(
        ReviewAdmissionApplicationDocumentCommand command,
        CancellationToken ct = default)
    {
        var document = await repository.FindApplicationDocumentAsync(
            command.PublicId, command.DocumentId, true, ct)
            ?? throw new KeyNotFoundException("Admission application document not found.");
        policy.EnsureCanReview(document.Status, command.Status, command.Observation);

        document.Status = command.Status;
        document.ReviewedAt = timeProvider.GetUtcNow().UtcDateTime;
        document.ReviewedByUserId = command.ReviewedByUserId;
        document.Observation = string.IsNullOrWhiteSpace(command.Observation)
            ? null
            : command.Observation.Trim();
        return GetAdmissionApplicationDocumentsQueryHandler.Map(
            command.PublicId,
            await repository.UpdateApplicationDocumentAsync(document, ct));
    }
}
