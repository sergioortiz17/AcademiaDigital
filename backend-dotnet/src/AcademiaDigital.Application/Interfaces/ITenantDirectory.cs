using AcademiaDigital.Application.DTOs;

namespace AcademiaDigital.Application.Interfaces;

/// <summary>Catálogo (en memoria, con refresco) de instituciones activas y sus dominios.</summary>
public interface ITenantDirectory
{
    /// <summary>Institución que atiende los requests sin dominio identificable (dev, Swagger, tests).</summary>
    TenantInfo Default { get; }
    Task<TenantInfo?> FindByHostAsync(string host, CancellationToken ct = default);
    /// <summary>Versión sincrónica para CORS: ¿el host pertenece a alguna institución activa?</summary>
    bool IsKnownHost(string host);
    void Invalidate();
}

public interface ITenantRegistry
{
    Task<IReadOnlyList<TenantSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    /// <summary>Devuelve los hosts de la lista que ya están asignados a alguna institución.</summary>
    Task<IReadOnlyList<string>> FindTakenHostsAsync(IReadOnlyCollection<string> hosts, CancellationToken ct = default);
}

public sealed record ProvisionTenantRequest(
    string Slug,
    string Name,
    string SchemaName,
    IReadOnlyList<string> Hosts,
    string AdminEmail,
    string AdminName,
    string AdminLastName,
    string AdminDni,
    string AdminPassword,
    string? TeacherEmailDomain = null);

/// <summary>Crea la base de la institución, aplica migraciones, siembra el admin y registra el tenant.</summary>
public interface ITenantProvisioner
{
    Task<TenantSummaryDto> ProvisionAsync(ProvisionTenantRequest request, CancellationToken ct = default);
}
