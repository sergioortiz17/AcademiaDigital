using AcademiaDigital.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configured = configuration["AdmissionStorage:RootPath"] ?? "data/admissions";
        _rootPath = Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured));
    }

    public async Task<string> SaveAsync(
        string storageKey,
        ReadOnlyMemory<byte> content,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        var normalizedKey = NormalizeKey(storageKey);
        var targetPath = Resolve(normalizedKey);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        var temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, content.ToArray(), ct);
            File.Move(temporaryPath, targetPath, true);
            return normalizedKey;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public async Task<StoredFile?> ReadAsync(
        string storageKey,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        var targetPath = Resolve(NormalizeKey(storageKey));
        if (!File.Exists(targetPath)) return null;
        return new StoredFile(await File.ReadAllBytesAsync(targetPath, ct), contentType, fileName);
    }

    private string Resolve(string storageKey)
    {
        var resolved = Path.GetFullPath(Path.Combine(_rootPath, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage key escapes the configured root.");
        return resolved;
    }

    private static string NormalizeKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("Storage key is required.");
        return storageKey.Replace('\\', '/').TrimStart('/');
    }
}
