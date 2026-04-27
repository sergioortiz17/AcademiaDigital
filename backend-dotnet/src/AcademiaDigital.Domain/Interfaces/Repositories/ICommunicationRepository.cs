using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICommunicationRepository
{
    Task<IEnumerable<Communication>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Communication>> GetActiveByTypeAsync(CommunicationType type, CancellationToken ct = default);
    Task<Communication?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<Communication> CreateAsync(Communication communication, CancellationToken ct = default);
    Task<Communication> UpdateAsync(Communication communication, CancellationToken ct = default);
    Task DeleteAsync(Communication communication, CancellationToken ct = default);
}
