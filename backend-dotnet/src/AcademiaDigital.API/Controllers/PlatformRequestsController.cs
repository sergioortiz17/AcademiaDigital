using AcademiaDigital.API.Filters;
using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.UseCases.Platform;
using AcademiaDigital.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AcademiaDigital.API.Controllers;

/// <summary>
/// Pedidos de plataforma ("Pide tu plataforma" en academiadigital.dryvn.org). Enviar es público;
/// ver y revisar los pedidos es del operador (admin.dryvn.org, clave X-Platform-Key).
/// No pertenece a ninguna institución (la ruta /api/v1/platform no se resuelve por tenant).
/// </summary>
[Route("api/v1/platform/requests")]
public class PlatformRequestsController(
    SubmitPlatformRequestHandler submit,
    ListPlatformRequestsHandler list,
    ReviewPlatformRequestHandler review,
    IConfiguration configuration) : ApiControllerBase
{
    // POST /api/v1/platform/requests  [público]
    [HttpPost]
    [EnableRateLimiting("PlatformRequest")]
    public async Task<IActionResult> Submit([FromBody] SubmitPlatformRequest request, CancellationToken ct)
    {
        await submit.Handle(request, configuration["Platform:NotifyEmail"], ct);
        // Misma respuesta aunque la trampa para bots descarte el pedido.
        return StatusCode(StatusCodes.Status201Created,
            new { success = true, msg = "Recibimos tu pedido. Te contactaremos a la brevedad." });
    }

    // GET /api/v1/platform/requests?status=Pending  [operador]
    [HttpGet]
    [PlatformAdminKey]
    [EnableRateLimiting("PlatformAdmin")]
    public async Task<ActionResult<IReadOnlyList<PlatformRequestDto>>> List(
        [FromQuery] PlatformRequestStatus? status, CancellationToken ct)
        => Ok(await list.Handle(status, ct));

    // PATCH /api/v1/platform/requests/{id}  { "status": 1, "tenantSlug": "usb" }  (1 = aprobado, 2 = rechazado)  [operador]
    [HttpPatch("{id:guid}")]
    [PlatformAdminKey]
    [EnableRateLimiting("PlatformAdmin")]
    public async Task<ActionResult<PlatformRequestDto>> Review(Guid id, [FromBody] ReviewPlatformRequest request, CancellationToken ct)
        => Ok(await review.Handle(id, request, ct));
}
