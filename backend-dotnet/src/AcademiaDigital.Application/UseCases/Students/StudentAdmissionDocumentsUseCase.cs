using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Students;

public sealed record AdmissionDocumentsDto(
    IReadOnlyList<DocumentRequirementDto> Requirements,
    IReadOnlyList<StudentDocumentDto> Documents);

/// <summary>
/// "Requisitos de ingreso": el alumno sube el analítico y los demás documentos que pide la
/// institución; Administración los revisa (aprueba o rechaza con observación).
/// </summary>
public sealed class StudentAdmissionDocumentsUseCase(
    IStudentRepository students,
    IStudentManagementService service,
    IStudentDocumentStorage storage,
    TimeProvider timeProvider)
{
    public async Task<AdmissionDocumentsDto> GetMineAsync(long userId, CancellationToken ct = default)
    {
        var student = await RequireStudent(userId, ct);
        var requirements = await service.GetRequirementsAsync(student.CareerId, ct);
        var documents = await service.GetDocumentsAsync(student.Id, ct);
        return new(requirements.Where(r => r.IsActive).ToArray(), documents);
    }

    public async Task<StudentDocumentDto> UploadMineAsync(
        long userId, int requirementId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var student = await RequireStudent(userId, ct);
        var contentType = UploadedDocumentPolicy.Validate(fileName, content);
        var requirement = (await service.GetRequirementsAsync(student.CareerId, ct)).FirstOrDefault(r => r.Id == requirementId && r.IsActive)
            ?? throw new KeyNotFoundException("El requisito no existe o no corresponde a su carrera.");
        // Clave legible para descargar desde el bucket: una carpeta por alumno y una por requisito.
        //   40123456_Perez_Ana/ANALITICO/20261122-153000_analitico.pdf
        var folder = UploadedDocumentPolicy.SafeFileName(
            $"{student.User.Dni ?? student.LegajoNumber}_{student.User.LastName}_{student.User.Username}");
        var stamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss");
        var key = $"{folder}/{UploadedDocumentPolicy.SafeFileName(requirement.Code)}/{stamp}_{UploadedDocumentPolicy.SafeFileName(fileName)}";
        var storedKey = await storage.SaveAsync(key, content, contentType, fileName, ct);
        return await service.AddDocumentAsync(student.Id, new CreateStudentDocumentRequest(
            requirement.Id, storedKey, fileName.Trim(), contentType, content.LongLength), ct);
    }

    /// <summary>Archivo de un documento: lo ve el alumno dueño o Administración.</summary>
    public async Task<StoredFile> ReadAsync(long studentId, long documentId, long userId, bool isAdmin, CancellationToken ct = default)
    {
        if (!isAdmin && !await service.IsOwnerAsync(studentId, userId, ct))
            throw new ForbiddenException("El documento pertenece a otro alumno.");
        var document = (await service.GetDocumentsAsync(studentId, ct)).FirstOrDefault(d => d.Id == documentId)
            ?? throw new KeyNotFoundException("Documento no encontrado.");
        return await storage.ReadAsync(document.FileUrl, document.ContentType, document.OriginalFileName, ct)
            ?? throw new KeyNotFoundException("El archivo del documento no está disponible.");
    }

    public async Task<long> GetMyStudentIdAsync(long userId, CancellationToken ct = default)
        => (await RequireStudent(userId, ct)).Id;

    private async Task<Domain.Entities.Student> RequireStudent(long userId, CancellationToken ct)
        => await students.FindByUserIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("El usuario actual no es un alumno.");
}
