using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Application.UseCases.Facturacion;

/// <summary>Consulta el padrón de ARCA por DNI o CUIT/CUIL y proyecta el resultado para la API.</summary>
public sealed class ConsultarPadronQueryHandler(IArcaClient arca)
{
    public async Task<PadronPersonaDto?> Handle(string documento, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(documento))
            throw new ArgumentException("El documento es obligatorio.");

        var persona = await arca.ConsultarPersonaAsync(documento, ct);
        if (persona is null) return null;

        return new PadronPersonaDto(
            persona.Cuit, persona.TipoPersona, persona.Denominacion, persona.EstadoClave,
            persona.CondicionIva, persona.Localidad, persona.Provincia, persona.CodigoPostal);
    }
}
