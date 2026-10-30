using AcademiaDigital.API.Models;
using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.UseCases.Facturacion;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>
/// Facturación electrónica contra ARCA (ex AFIP). Solo Admin: la emisión de comprobantes es una
/// operación administrativa. El frontend lo construyen los alumnos; acá queda el backend completo.
/// </summary>
[Route("api/v1/comprobantes")]
public sealed class ComprobantesController(
    EmitirComprobanteCommandHandler emitirHandler,
    GetComprobantesQueryHandler getComprobantesHandler,
    GetComprobanteByIdQueryHandler getByIdHandler,
    ConsultarPadronQueryHandler padronHandler,
    GetUltimoComprobanteQueryHandler ultimoHandler) : ApiControllerBase
{
    /// <summary>Emite (solicita CAE de) un nuevo comprobante electrónico.</summary>
    [HttpPost]
    public async Task<IActionResult> Emitir([FromBody] EmitirComprobanteRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;

        var comprobante = await emitirHandler.Handle(request, CurrentUserId!.Value, ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, comprobante });
    }

    /// <summary>Lista los comprobantes persistidos, con filtros opcionales por estado y punto de venta.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ComprobanteEstado? estado,
        [FromQuery] int? puntoVenta,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;

        var comprobantes = await getComprobantesHandler.Handle(estado, puntoVenta, ct);
        return Ok(new { success = true, comprobantes });
    }

    /// <summary>Detalle de un comprobante por id.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;

        var comprobante = await getByIdHandler.Handle(id, ct);
        return comprobante is null
            ? NotFound(ApiResponse.Fail("Comprobante no encontrado."))
            : Ok(new { success = true, comprobante });
    }

    /// <summary>Consulta el padrón de ARCA por DNI (7-8 dígitos) o CUIT/CUIL (11 dígitos).</summary>
    [HttpGet("padron/{documento}")]
    public async Task<IActionResult> ConsultarPadron(string documento, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;

        var persona = await padronHandler.Handle(documento, ct);
        return persona is null
            ? NotFound(ApiResponse.Fail("No se encontró la persona en el padrón de ARCA."))
            : Ok(new { success = true, persona });
    }

    /// <summary>Número del último comprobante autorizado por ARCA (para previsualizar el próximo).</summary>
    [HttpGet("ultimo")]
    public async Task<IActionResult> GetUltimo(
        [FromQuery] int tipoComprobante,
        [FromQuery] int? puntoVenta,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;

        var ultimo = await ultimoHandler.Handle(tipoComprobante, puntoVenta, ct);
        return Ok(new { success = true, ultimo });
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
    }
}
