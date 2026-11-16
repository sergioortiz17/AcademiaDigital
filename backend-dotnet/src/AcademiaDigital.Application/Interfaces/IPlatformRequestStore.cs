using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Application.Interfaces;

/// <summary>Pedidos de plataforma en el esquema de control.</summary>
public interface IPlatformRequestStore
{
    Task AddAsync(PlatformRequest request, CancellationToken ct = default);
    Task<PlatformRequest?> FindAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformRequest>> ListAsync(PlatformRequestStatus? status, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
