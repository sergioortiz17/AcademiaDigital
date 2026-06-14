using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Course>> GetByCareerAsync(int careerId, CancellationToken ct = default);
    Task<Course?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<Course?> FindByCodeAsync(int careerId, string code, CancellationToken ct = default);
    Task<bool> ExistsInCareerAsync(int courseId, int careerId, CancellationToken ct = default);
    Task<Course> CreateAsync(Course course, CancellationToken ct = default);
    Task<Course> UpdateAsync(Course course, CancellationToken ct = default);
    Task DeleteAsync(Course course, CancellationToken ct = default);
}
