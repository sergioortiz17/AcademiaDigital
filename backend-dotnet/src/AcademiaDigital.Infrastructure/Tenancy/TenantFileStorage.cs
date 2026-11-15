using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Infrastructure.Tenancy;

/// <summary>
/// Separa los archivos por institución anteponiendo <c>tenants/{slug}/</c> a la clave lógica.
/// La institución por defecto conserva las claves sin prefijo para no romper los archivos que
/// ya existen (certificados, recibos, acuerdos).
/// </summary>
public sealed class TenantFileStorage(IFileStorage inner, ITenantContext tenant) : IFileStorage
{
    public async Task<string> SaveAsync(string storageKey, ReadOnlyMemory<byte> content, string contentType,
        string fileName, CancellationToken ct = default)
        => Unscope(await inner.SaveAsync(Scope(storageKey), content, contentType, fileName, ct));

    public Task<StoredFile?> ReadAsync(string storageKey, string contentType, string fileName, CancellationToken ct = default)
        => inner.ReadAsync(Scope(storageKey), contentType, fileName, ct);

    private string Prefix => tenant.Current.IsDefault ? string.Empty : $"tenants/{tenant.Current.Slug}/";

    private string Scope(string key) => Prefix + key;

    private string Unscope(string key) => Prefix.Length > 0 && key.StartsWith(Prefix, StringComparison.Ordinal)
        ? key[Prefix.Length..]
        : key;
}
