using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Tenants;

public sealed class GetTenantsQueryHandler(ITenantRegistry registry)
{
    public Task<IReadOnlyList<TenantSummaryDto>> Handle(CancellationToken ct = default)
        => registry.ListAsync(ct);
}

public sealed class CreateTenantCommandHandler(
    TenantPolicy policy,
    ITenantRegistry registry,
    ITenantProvisioner provisioner)
{
    public async Task<TenantSummaryDto> Handle(CreateTenantRequest request, CancellationToken ct = default)
    {
        var slug = policy.NormalizeSlug(request.Slug);
        var name = policy.NormalizeName(request.Name);
        var hosts = policy.NormalizeHosts(request.Domains);
        policy.ValidateAdmin(request.AdminEmail, request.AdminName, request.AdminLastName,
            request.AdminDni, request.AdminPassword);

        if (await registry.SlugExistsAsync(slug, ct))
            throw new InvalidOperationException($"Ya existe una institución con el identificador '{slug}'.");
        var taken = await registry.FindTakenHostsAsync(hosts, ct);
        if (taken.Count > 0)
            throw new InvalidOperationException($"Los dominios ya están asignados a otra institución: {string.Join(", ", taken)}.");

        return await provisioner.ProvisionAsync(new ProvisionTenantRequest(
            slug, name, policy.DatabaseNameFor(slug), hosts,
            request.AdminEmail.Trim(), request.AdminName.Trim(), request.AdminLastName.Trim(),
            request.AdminDni.Trim(), request.AdminPassword), ct);
    }
}
