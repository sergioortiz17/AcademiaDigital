using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Application.UseCases.Facturacion;

/// <summary>Consulta a ARCA el número del último comprobante autorizado (para previsualizar el próximo).</summary>
public sealed class GetUltimoComprobanteQueryHandler(IArcaClient arca)
{
    public Task<long> Handle(int tipoComprobante, int? puntoVenta, CancellationToken ct = default)
        => arca.ObtenerUltimoComprobanteAsync(puntoVenta ?? arca.PuntoVentaPorDefecto, tipoComprobante, ct);
}
