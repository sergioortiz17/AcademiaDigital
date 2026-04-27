using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ISessionRepository
{
    Task<ActiveSession?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task<ActiveSession?> FindByUserAsync(long userId, CancellationToken ct = default);
    Task<ActiveSession> CreateAsync(long userId, string token, CancellationToken ct = default);
    Task DeleteAsync(ActiveSession session, CancellationToken ct = default);
    Task DeleteByUserAsync(long userId, CancellationToken ct = default);
}
