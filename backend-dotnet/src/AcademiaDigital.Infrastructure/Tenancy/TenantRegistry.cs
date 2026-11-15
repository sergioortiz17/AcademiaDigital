using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence.Control;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcademiaDigital.Infrastructure.Tenancy;

public sealed class TenantRegistry(ControlDbContext control, IOptions<TenancyOptions> options) : ITenantRegistry
{
    public async Task<IReadOnlyList<TenantSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var tenants = await control.Tenants.AsNoTracking().Include(t => t.Domains)
            .OrderBy(t => t.CreatedAt).ToListAsync(ct);
        return tenants.Select(t => ToDto(t, options.Value.DefaultSlug)).ToArray();
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => control.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task<IReadOnlyList<string>> FindTakenHostsAsync(IReadOnlyCollection<string> hosts, CancellationToken ct = default)
        => await control.TenantDomains.Where(d => hosts.Contains(d.Host)).Select(d => d.Host).ToListAsync(ct);

    internal static TenantSummaryDto ToDto(Tenant tenant, string defaultSlug) => new(
        tenant.Id, tenant.Slug, tenant.Name, tenant.IsActive,
        string.Equals(tenant.Slug, defaultSlug, StringComparison.Ordinal), tenant.CreatedAt,
        tenant.Domains.Select(d => d.Host).OrderBy(h => h, StringComparer.Ordinal).ToArray());
}
