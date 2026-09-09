using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Teachers;

public sealed record GetTeacherDocumentsQuery(long TeacherId);

public sealed record SubmitTeacherDocumentCommand(
    long TeacherId,
    string DocumentType,
    string FileUrl,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateOnly? ValidUntil);

public sealed record ReviewTeacherDocumentCommand(
    long TeacherId,
    long DocumentId,
    StudentDocumentStatus Status,
    string? Observation,
    long ReviewedByUserId);

public sealed record TeacherDocumentDto(
    long Id,
    long TeacherId,
    string DocumentType,
    int Version,
    string FileUrl,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Status,
    DateTime SubmittedAt,
    DateOnly? ValidUntil,
    DateTime? ReviewedAt,
    long? ReviewedByUserId,
    string? Observation);

public sealed class GetTeacherDocumentsQueryHandler(
    ITeacherRepository teacherRepository,
    ITeacherDocumentRepository documentRepository)
{
    public async Task<IReadOnlyList<TeacherDocumentDto>> Handle(
        GetTeacherDocumentsQuery query,
        CancellationToken ct = default)
    {
        _ = await teacherRepository.FindByIdAsync(query.TeacherId, ct)
            ?? throw new KeyNotFoundException("Docente no encontrado.");
        return (await documentRepository.GetByTeacherAsync(query.TeacherId, ct))
            .Select(TeacherDocumentMapper.Map)
            .ToArray();
    }
}

public sealed class SubmitTeacherDocumentCommandHandler(
    ITeacherDocumentRepository repository,
    TeacherDocumentPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<TeacherDocumentDto> Handle(
        SubmitTeacherDocumentCommand command,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var documentType = policy.NormalizeDocumentType(command.DocumentType);
        policy.ValidateSubmission(
            command.FileUrl,
            command.OriginalFileName,
            command.ContentType,
            command.FileSizeBytes,
            command.ValidUntil,
            DateOnly.FromDateTime(now));

        var document = new TeacherDocument
        {
            TeacherId = command.TeacherId,
            DocumentType = documentType,
            FileUrl = command.FileUrl.Trim(),
            OriginalFileName = command.OriginalFileName.Trim(),
            ContentType = command.ContentType.Trim().ToLowerInvariant(),
            FileSizeBytes = command.FileSizeBytes,
            Status = StudentDocumentStatus.Submitted,
            SubmittedAt = now,
            ValidUntil = command.ValidUntil
        };
        var created = await unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCt => repository.CreateVersionAsync(document, transactionCt), ct);
        return TeacherDocumentMapper.Map(created);
    }
}

public sealed class ReviewTeacherDocumentCommandHandler(
    ITeacherDocumentRepository repository,
    TeacherDocumentPolicy policy,
    TimeProvider timeProvider)
{
    public async Task<TeacherDocumentDto> Handle(
        ReviewTeacherDocumentCommand command,
        CancellationToken ct = default)
    {
        var document = await repository.FindAsync(command.TeacherId, command.DocumentId, true, ct)
            ?? throw new KeyNotFoundException("Documento del docente no encontrado.");
        policy.EnsureCanReview(document.Status, command.Status, command.Observation);

        document.Status = command.Status;
        document.ReviewedAt = timeProvider.GetUtcNow().UtcDateTime;
        document.ReviewedByUserId = command.ReviewedByUserId;
        document.Observation = string.IsNullOrWhiteSpace(command.Observation)
            ? null
            : command.Observation.Trim();
        return TeacherDocumentMapper.Map(await repository.UpdateAsync(document, ct));
    }
}

internal static class TeacherDocumentMapper
{
    public static TeacherDocumentDto Map(TeacherDocument document) => new(
        document.Id,
        document.TeacherId,
        document.DocumentType,
        document.Version,
        document.FileUrl,
        document.OriginalFileName,
        document.ContentType,
        document.FileSizeBytes,
        document.Status.ToString(),
        document.SubmittedAt,
        document.ValidUntil,
        document.ReviewedAt,
        document.ReviewedByUserId,
        document.Observation);
}
