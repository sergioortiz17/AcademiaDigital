using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudentRepository
{
    Task<IEnumerable<Student>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Student>> GetByCareerAsync(int careerId, CancellationToken ct = default);
    Task<Student?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<Student?> FindByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Student?> FindByLegajoAsync(string legajoNumber, CancellationToken ct = default);
    Task<Student> CreateAsync(Student student, CancellationToken ct = default);
    Task<Student> UpdateAsync(Student student, CancellationToken ct = default);
    Task DeleteAsync(Student student, CancellationToken ct = default);
}
