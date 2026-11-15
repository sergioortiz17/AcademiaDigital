using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Infrastructure.Persistence.Control;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AcademiaDigital.Infrastructure.Tenancy;

/// <summary>
/// Catálogo en memoria host → institución. Se recarga cada 30 s, al invalidarlo y, si llega un
/// host desconocido, como máximo cada 5 s (así una institución creada desde otra instancia se
/// ve rápido sin que un host inexistente golpee la base en cada request).
/// La recarga es sincrónica a propósito: la tabla es mínima y CORS necesita una consulta sync.
/// </summary>
public sealed class TenantDirectory(IServiceScopeFactory scopeFactory, IOptions<TenancyOptions> options) : ITenantDirectory
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MissRefreshAfter = TimeSpan.FromSeconds(5);

    private readonly object _gate = new();
    private Snapshot? _snapshot;

    public TenantInfo Default
        => Current().DefaultTenant
            ?? throw new InvalidOperationException(
                $"La institución por defecto '{options.Value.DefaultSlug}' no está registrada o está inactiva.");

    public Task<TenantInfo?> FindByHostAsync(string host, CancellationToken ct = default)
    {
        var snapshot = Current();
        if (snapshot.ByHost.TryGetValue(host, out var tenant)) return Task.FromResult<TenantInfo?>(tenant);

        // Miss: puede ser una institución recién creada en otra instancia.
        if (DateTime.UtcNow - snapshot.LoadedAt > MissRefreshAfter)
            snapshot = Refresh();
        return Task.FromResult(snapshot.ByHost.GetValueOrDefault(host));
    }

    public bool IsKnownHost(string host)
    {
        var snapshot = Current();
        if (snapshot.ByHost.ContainsKey(host)) return true;
        if (DateTime.UtcNow - snapshot.LoadedAt > MissRefreshAfter)
            snapshot = Refresh();
        return snapshot.ByHost.ContainsKey(host);
    }

    public void Invalidate()
    {
        lock (_gate) _snapshot = null;
    }

    private Snapshot Current()
    {
        var snapshot = _snapshot;
        return snapshot is not null && DateTime.UtcNow - snapshot.LoadedAt < MaxAge ? snapshot : Refresh();
    }

    private Snapshot Refresh()
    {
        lock (_gate)
        {
            using var scope = scopeFactory.CreateScope();
            var control = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
            var tenants = control.Tenants.AsNoTracking().Include(t => t.Domains).Where(t => t.IsActive).ToList();

            var byHost = new Dictionary<string, TenantInfo>(StringComparer.Ordinal);
            TenantInfo? defaultTenant = null;
            foreach (var tenant in tenants)
            {
                var info = new TenantInfo(tenant.Id, tenant.Slug, tenant.Name, tenant.DatabaseName,
                    string.Equals(tenant.Slug, options.Value.DefaultSlug, StringComparison.Ordinal),
                    tenant.TeacherEmailDomain);
                if (info.IsDefault) defaultTenant = info;
                foreach (var domain in tenant.Domains) byHost[domain.Host] = info;
            }

            return _snapshot = new Snapshot(byHost, defaultTenant, DateTime.UtcNow);
        }
    }

    private sealed record Snapshot(IReadOnlyDictionary<string, TenantInfo> ByHost, TenantInfo? DefaultTenant, DateTime LoadedAt);
}
