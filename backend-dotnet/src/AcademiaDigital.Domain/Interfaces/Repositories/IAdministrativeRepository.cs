using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IAdministrativeRepository
{
    Task<IEnumerable<Administrative>> GetAllAsync(CancellationToken ct = default);
    Task<Administrative?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<Administrative?> FindByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Administrative?> FindByEmployeeNumberAsync(string employeeNumber, CancellationToken ct = default);
    Task<Administrative> CreateAsync(Administrative administrative, CancellationToken ct = default);
    Task<Administrative> UpdateAsync(Administrative administrative, CancellationToken ct = default);
    Task DeleteAsync(Administrative administrative, CancellationToken ct = default);
}
