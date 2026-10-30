using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IComprobanteRepository
{
    Task<Comprobante> CreateAsync(Comprobante comprobante, CancellationToken ct = default);
    Task<Comprobante?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<Comprobante?> FindForUpdateAsync(long id, CancellationToken ct = default);
    Task<List<Comprobante>> GetAllAsync(ComprobanteEstado? estado, int? puntoVenta, CancellationToken ct = default);
}
