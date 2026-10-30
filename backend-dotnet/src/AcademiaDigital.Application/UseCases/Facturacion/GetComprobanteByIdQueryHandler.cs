using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Facturacion;

/// <summary>Devuelve un comprobante persistido por su id, o null si no existe.</summary>
public sealed class GetComprobanteByIdQueryHandler(IComprobanteRepository repository)
{
    public async Task<ComprobanteDto?> Handle(long id, CancellationToken ct = default)
    {
        var comprobante = await repository.FindByIdAsync(id, ct);
        return comprobante is null ? null : ComprobanteDto.From(comprobante);
    }
}
