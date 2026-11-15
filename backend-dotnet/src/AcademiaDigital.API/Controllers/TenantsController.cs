using AcademiaDigital.API.Filters;
using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AcademiaDigital.API.Controllers;

/// <summary>Datos públicos de la institución del dominio actual (nombre para el login y el título).</summary>
[Route("api/v1/tenant")]
public class TenantController(ITenantContext tenant) : ApiControllerBase
{
    [HttpGet("current")]
    public ActionResult<CurrentTenantDto> GetCurrent()
        => Ok(new CurrentTenantDto(tenant.Current.Slug, tenant.Current.Name, tenant.Current.TeacherEmailDomain));
}

/// <summary>
/// Administración de instituciones (operador de la plataforma). No pertenece a ninguna
/// institución: se autentica con <c>X-Platform-Key</c>, no con una sesión de usuario.
/// </summary>
[Route("api/v1/platform/tenants")]
[PlatformAdminKey]
[EnableRateLimiting("PlatformAdmin")]
public class PlatformTenantsController(
    GetTenantsQueryHandler getTenants,
    CreateTenantCommandHandler createTenant) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryDto>>> List(CancellationToken ct)
        => Ok(await getTenants.Handle(ct));

    [HttpPost]
    public async Task<ActionResult<TenantSummaryDto>> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var tenant = await createTenant.Handle(request, ct);
        return Created($"api/v1/platform/tenants/{tenant.Slug}", tenant);
    }
}
