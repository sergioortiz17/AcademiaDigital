using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Facturacion;

/// <summary>
/// Emite un comprobante electrónico: persiste primero el pedido en estado Pendiente (auditoría),
/// pide el CAE a ARCA FUERA de una transacción de base de datos (es una llamada de red externa) y
/// luego actualiza el comprobante con el CAE o con el detalle del rechazo.
/// </summary>
public sealed class EmitirComprobanteCommandHandler(
    IComprobanteRepository repository,
    IArcaClient arca,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ComprobanteDto> Handle(EmitirComprobanteRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (request.Subtotal < 0 || request.Percepcion < 0 || request.Total < 0)
            throw new ArgumentException("Los importes no pueden ser negativos.");
        if (request.Concepto is < 1 or > 3)
            throw new ArgumentException("El concepto debe ser 1 (Productos), 2 (Servicios) o 3 (Ambos).");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var puntoVenta = request.PuntoVenta ?? arca.PuntoVentaPorDefecto;

        var comprobante = new Comprobante
        {
            PuntoVenta = puntoVenta,
            TipoComprobante = request.TipoComprobante,
            Concepto = request.Concepto,
            CondicionIvaReceptorId = request.CondicionIvaReceptorId,
            DocumentoReceptor = request.DocumentoReceptor,
            FechaEmision = request.FechaEmision,
            FechaServicioDesde = request.FechaServicioDesde,
            FechaServicioHasta = request.FechaServicioHasta,
            FechaVencimientoPago = request.FechaVencimientoPago,
            Subtotal = request.Subtotal,
            Percepcion = request.Percepcion,
            Total = request.Total,
            Estado = ComprobanteEstado.Pendiente,
            SolicitadoPorUserId = actorUserId,
            CreatedAt = now
        };

        // Persistimos el pedido antes de llamar a ARCA para dejar rastro aunque la emisión falle.
        await repository.CreateAsync(comprobante, ct);

        var arcaRequest = new ArcaAutorizacionRequest
        {
            Concepto = request.Concepto,
            CondicionIVAReceptorId = request.CondicionIvaReceptorId,
            DocumentoReceptor = request.DocumentoReceptor,
            FechaEmision = request.FechaEmision,
            FechaServicioDesde = request.FechaServicioDesde,
            FechaServicioHasta = request.FechaServicioHasta,
            FechaVencimientoPago = request.FechaVencimientoPago ?? request.FechaEmision,
            Subtotal = request.Subtotal,
            Percepcion = request.Percepcion,
            Total = request.Total,
            ComprobanteAsociado = request.ComprobanteAsociado is { } asoc
                ? new ArcaComprobanteAsociado { Tipo = asoc.Tipo, PtoVta = asoc.PtoVta, Nro = asoc.Nro, CbteFch = asoc.CbteFch }
                : null
        };

        // El adaptador nunca lanza por rechazos de negocio: siempre devuelve un resultado.
        var result = await arca.AutorizarComprobanteAsync(puntoVenta, request.TipoComprobante, arcaRequest, ct);

        if (result.Autorizado && !string.IsNullOrWhiteSpace(result.CAE))
            comprobante.MarcarAutorizado(result.CAE!, result.CAEFechaVencimiento, result.NumeroComprobante, now);
        else
            comprobante.MarcarRechazado(result.MensajeError);

        await unitOfWork.SaveChangesAsync(ct);
        return ComprobanteDto.From(comprobante);
    }
}
