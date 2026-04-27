using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICareerRepository
{
    Task<IEnumerable<Career>> GetAllAsync(CancellationToken ct = default);
    Task<Career?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<Career?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<Career> CreateAsync(Career career, CancellationToken ct = default);
    Task<Career> UpdateAsync(Career career, CancellationToken ct = default);
    Task DeleteAsync(Career career, CancellationToken ct = default);
}
