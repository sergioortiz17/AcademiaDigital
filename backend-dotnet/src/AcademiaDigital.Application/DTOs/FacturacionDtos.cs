using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Application.DTOs;

/// <summary>Datos de entrada para emitir (solicitar CAE de) un comprobante electrónico.</summary>
public sealed record EmitirComprobanteRequest(
    int TipoComprobante,
    int Concepto,
    int CondicionIvaReceptorId,
    string? DocumentoReceptor,
    DateTime FechaEmision,
    decimal Subtotal,
    decimal Percepcion,
    decimal Total,
    int? PuntoVenta = null,
    DateTime? FechaServicioDesde = null,
    DateTime? FechaServicioHasta = null,
    DateTime? FechaVencimientoPago = null,
    ComprobanteAsociadoDto? ComprobanteAsociado = null);

public sealed record ComprobanteAsociadoDto(int Tipo, int PtoVta, long Nro, string CbteFch);

/// <summary>Proyección de un comprobante persistido para exponer por la API.</summary>
public sealed record ComprobanteDto(
    long Id,
    int PuntoVenta,
    int TipoComprobante,
    int Concepto,
    long? NumeroComprobante,
    int CondicionIvaReceptorId,
    string? DocumentoReceptor,
    DateTime FechaEmision,
    decimal Subtotal,
    decimal Percepcion,
    decimal Total,
    string Estado,
    string? Cae,
    DateOnly? CaeFechaVencimiento,
    string? MensajeError,
    DateTime CreatedAt,
    DateTime? AutorizadoAt)
{
    public static ComprobanteDto From(Comprobante c) => new(
        c.Id, c.PuntoVenta, c.TipoComprobante, c.Concepto, c.NumeroComprobante,
        c.CondicionIvaReceptorId, c.DocumentoReceptor, c.FechaEmision,
        c.Subtotal, c.Percepcion, c.Total, c.Estado.ToString(),
        c.Cae, c.CaeFechaVencimiento, c.MensajeError, c.CreatedAt, c.AutorizadoAt);
}

/// <summary>Datos de una persona del padrón de ARCA, listos para exponer por la API.</summary>
public sealed record PadronPersonaDto(
    long Cuit,
    string TipoPersona,
    string Denominacion,
    string EstadoClave,
    string? CondicionIva,
    string? Localidad,
    string? Provincia,
    string? CodigoPostal);
