using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Services;
using AcademiaDigital.Infrastructure.Persistence.Control;
using AcademiaDigital.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AcademiaDigital.Infrastructure.Tenancy;

/// <summary>
/// Alta de una institución. El registro en la base de control es el ÚLTIMO paso (punto de
/// confirmación): si algo falla antes, la institución no existe para nadie y se puede reintentar
/// con los mismos datos, porque crear/migrar la base y sembrar el admin son idempotentes.
/// Nunca se borra una base automáticamente.
/// </summary>
public sealed class TenantProvisioner(
    ControlDbContext control,
    TenantDbContextFactory dbFactory,
    IPasswordHasher passwordHasher,
    ITenantDirectory directory,
    IOptions<TenancyOptions> options,
    TimeProvider timeProvider,
    ILogger<TenantProvisioner> logger) : ITenantProvisioner
{
    public async Task<TenantSummaryDto> ProvisionAsync(ProvisionTenantRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Alta de institución {Slug}: creando y migrando la base {Database}.", request.Slug, request.DatabaseName);

        await using (var db = dbFactory.Create(request.DatabaseName))
        {
            await db.Database.MigrateAsync(ct); // crea la base si no existe
            var users = new UserRepository(db, passwordHasher);
            if (await users.FindByEmailAsync(request.AdminEmail, ct) is null)
                await users.CreateAsync(request.AdminEmail, request.AdminName, request.AdminLastName,
                    request.AdminPassword, request.AdminDni, UserRole.Admin, ct);
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Name = request.Name,
            DatabaseName = request.DatabaseName,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            TeacherEmailDomain = request.TeacherEmailDomain,
            Domains = request.Hosts.Select(host => new TenantDomain { Id = Guid.NewGuid(), Host = host }).ToList()
        };
        control.Tenants.Add(tenant);
        try
        {
            await control.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Carrera con otra alta del mismo identificador o dominio (índices únicos).
            throw new InvalidOperationException("El identificador o alguno de los dominios ya está registrado.");
        }

        directory.Invalidate();
        logger.LogInformation("Institución {Slug} creada con {Count} dominio(s).", request.Slug, request.Hosts.Count);
        return TenantRegistry.ToDto(tenant, options.Value.DefaultSlug);
    }
}
