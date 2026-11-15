using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence.Control;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AcademiaDigital.Infrastructure.Tenancy;

public static class TenancyInitializer
{
    /// <summary>
    /// Arranque multitenant: migra la base de control, registra la institución por defecto
    /// (apuntando a la base que ya existía, sin mover datos) y migra las demás instituciones.
    /// La base de la institución por defecto la migra Program.cs con su lógica de reintentos.
    /// </summary>
    public static async Task InitializeTenancyAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Tenancy");
        var options = scope.ServiceProvider.GetRequiredService<IOptions<TenancyOptions>>().Value;
        var connections = scope.ServiceProvider.GetRequiredService<TenantConnectionStrings>();
        var control = scope.ServiceProvider.GetRequiredService<ControlDbContext>();

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await control.Database.MigrateAsync(ct);
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts
                && ex is NpgsqlException or TimeoutException or System.Net.Sockets.SocketException)
            {
                logger.LogWarning(ex, "Sin conexión a PostgreSQL (intento {Attempt}/{Max}), reintentando en 3s...", attempt, maxAttempts);
                await Task.Delay(3000, ct);
            }
        }

        await EnsureDefaultTenantAsync(control, options, connections, logger, ct);
        services.GetRequiredService<ITenantDirectory>().Invalidate();

        var dbFactory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();
        var others = await control.Tenants.AsNoTracking()
            .Where(t => t.IsActive && t.Slug != options.DefaultSlug).ToListAsync(ct);
        foreach (var tenant in others)
        {
            try
            {
                await using var db = dbFactory.Create(tenant.DatabaseName);
                await db.Database.MigrateAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Una institución rota no debe impedir que arranquen las demás.
                logger.LogError(ex, "No se pudo migrar la base de la institución {Slug}.", tenant.Slug);
            }
        }
    }

    private static async Task EnsureDefaultTenantAsync(
        ControlDbContext control, TenancyOptions options, TenantConnectionStrings connections,
        ILogger logger, CancellationToken ct)
    {
        var tenant = await control.Tenants.Include(t => t.Domains)
            .FirstOrDefaultAsync(t => t.Slug == options.DefaultSlug, ct);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Slug = options.DefaultSlug,
                Name = options.DefaultName,
                DatabaseName = connections.DefaultDatabaseName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            control.Tenants.Add(tenant);
            logger.LogInformation("Registrando la institución por defecto {Slug} sobre la base {Database}.",
                tenant.Slug, tenant.DatabaseName);
        }

        // Solo agrega hosts faltantes (y que no pertenezcan a otra institución); nunca quita.
        var wanted = options.DefaultDomains
            .Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim().ToLowerInvariant()).Distinct().ToArray();
        var takenElsewhere = await control.TenantDomains
            .Where(d => wanted.Contains(d.Host) && d.TenantId != tenant.Id).Select(d => d.Host).ToListAsync(ct);
        foreach (var host in wanted.Except(tenant.Domains.Select(d => d.Host)).Except(takenElsewhere))
            tenant.Domains.Add(new TenantDomain { Id = Guid.NewGuid(), Host = host });

        await control.SaveChangesAsync(ct);
    }
}
