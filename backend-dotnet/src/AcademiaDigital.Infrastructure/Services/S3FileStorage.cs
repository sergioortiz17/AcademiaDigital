using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using AcademiaDigital.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AcademiaDigital.Infrastructure.Services;

/// <summary>
/// IFileStorage sobre Amazon S3: acuerdos, certificados y recibos quedan en un bucket y no
/// en el disco de la instancia, así cualquier instancia de Beanstalk los lee y sobreviven a
/// su reemplazo. Credenciales: el rol de la instancia (sin claves en la configuración).
///
/// La clave lógica que guarda la base (por ejemplo "certificates/2026/CERT-00000001.pdf")
/// es la misma que con LocalFileStorage; S3 solo le antepone Storage:S3:Prefix
/// ("backend/"), para que el monolito y Finance compartan el bucket sin pisarse.
/// </summary>
public sealed class S3FileStorage(IAmazonS3 s3, IConfiguration configuration) : IFileStorage
{
    private readonly string _bucket = configuration["Storage:S3:Bucket"]
        ?? throw new InvalidOperationException("Storage:S3:Bucket es obligatorio con Storage:Provider=S3.");

    private readonly string _prefix = NormalizePrefix(configuration["Storage:S3:Prefix"]);

    public async Task<string> SaveAsync(
        string storageKey,
        ReadOnlyMemory<byte> content,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        var normalizedKey = StorageKey.Normalize(storageKey);
        using var stream = new MemoryStream(content.ToArray(), writable: false);

        // PutObject es atómico: el objeto aparece completo o no aparece.
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = _prefix + normalizedKey,
            InputStream = stream,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        }, ct);

        return normalizedKey;
    }

    public async Task<StoredFile?> ReadAsync(
        string storageKey,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await s3.GetObjectAsync(_bucket, _prefix + StorageKey.Normalize(storageKey), ct);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, ct);
            return new StoredFile(buffer.ToArray(), contentType, fileName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null; // mismo contrato que LocalFileStorage: archivo inexistente → null
        }
    }

    private static string NormalizePrefix(string? prefix)
    {
        var trimmed = (prefix ?? string.Empty).Trim().Trim('/');
        return trimmed.Length == 0 ? string.Empty : trimmed + "/";
    }
}

/// <summary>Validación de claves lógicas, común a los adaptadores de almacenamiento.</summary>
internal static class StorageKey
{
    public static string Normalize(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("Storage key is required.");

        var normalized = storageKey.Replace('\\', '/').TrimStart('/');
        var segments = normalized.Split('/');
        if (segments.Any(s => s.Length == 0 || s == "." || s == ".."))
            throw new InvalidOperationException("Storage key escapes the configured root.");

        return normalized;
    }
}
