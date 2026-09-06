namespace AcademiaDigital.Finance.Application.Interfaces;

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
