using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Infrastructure.Services;

/// <summary>Adapta un IFileStorage al almacenamiento de documentos de alumnos.</summary>
public sealed class StudentDocumentStorage(IFileStorage inner) : IStudentDocumentStorage
{
    public Task<string> SaveAsync(string storageKey, ReadOnlyMemory<byte> content, string contentType, string fileName, CancellationToken ct = default)
        => inner.SaveAsync(storageKey, content, contentType, fileName, ct);

    public Task<StoredFile?> ReadAsync(string storageKey, string contentType, string fileName, CancellationToken ct = default)
        => inner.ReadAsync(storageKey, contentType, fileName, ct);
}
