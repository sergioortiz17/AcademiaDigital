using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ITeacherRepository
{
    Task<IEnumerable<Teacher>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Teacher?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<Teacher?> FindByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Teacher?> FindByEmployeeNumberAsync(string employeeNumber, CancellationToken ct = default);
    Task<Teacher> CreateAsync(Teacher teacher, CancellationToken ct = default);
    Task<Teacher> UpdateAsync(Teacher teacher, CancellationToken ct = default);
    Task DeleteAsync(Teacher teacher, CancellationToken ct = default);
}
