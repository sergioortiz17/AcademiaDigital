using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICooperativeEntityRepository
{
    Task<IEnumerable<CooperativeEntity>> GetAllAsync(CancellationToken ct = default);
    Task<CooperativeEntity?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<CooperativeEntity?> FindByCuitAsync(string cuit, CancellationToken ct = default);
    Task<CooperativeEntity> CreateAsync(CooperativeEntity entity, CancellationToken ct = default);
    Task<CooperativeEntity> UpdateAsync(CooperativeEntity entity, CancellationToken ct = default);
    Task DeleteAsync(CooperativeEntity entity, CancellationToken ct = default);
}
