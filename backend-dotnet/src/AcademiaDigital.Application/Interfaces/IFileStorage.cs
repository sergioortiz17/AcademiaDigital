namespace AcademiaDigital.Application.Interfaces;

public sealed record StoredFile(byte[] Content, string ContentType, string FileName);

public interface IFileStorage
{
    Task<string> SaveAsync(
        string storageKey,
        ReadOnlyMemory<byte> content,
        string contentType,
        string fileName,
        CancellationToken ct = default);
    Task<StoredFile?> ReadAsync(
        string storageKey,
        string contentType,
        string fileName,
        CancellationToken ct = default);
}

/// <summary>
/// Almacenamiento de los documentos que suben los alumnos (requisitos de ingreso). En AWS es un
/// bucket propio; en desarrollo, el mismo almacenamiento que el resto de los archivos.
/// </summary>
public interface IStudentDocumentStorage : IFileStorage;
