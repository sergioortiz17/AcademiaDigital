using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Facturacion;

/// <summary>Lista los comprobantes persistidos, con filtros opcionales por estado y punto de venta.</summary>
public sealed class GetComprobantesQueryHandler(IComprobanteRepository repository)
{
    public async Task<IReadOnlyList<ComprobanteDto>> Handle(
        ComprobanteEstado? estado, int? puntoVenta, CancellationToken ct = default)
    {
        var comprobantes = await repository.GetAllAsync(estado, puntoVenta, ct);
        return comprobantes.Select(ComprobanteDto.From).ToArray();
    }
}
