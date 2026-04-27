using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ITeachingPositionRepository
{
    Task<IEnumerable<TeachingPosition>> GetBySubjectAsync(int subjectId, CancellationToken ct = default);
    Task<IEnumerable<TeachingPosition>> GetByTeacherAsync(long teacherId, CancellationToken ct = default);
    Task<IEnumerable<TeachingPosition>> GetByPeriodAsync(int year, int semester, CancellationToken ct = default);
    Task<IEnumerable<TeachingPosition>> GetVacantAsync(CancellationToken ct = default);
    Task<TeachingPosition?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<TeachingPosition> CreateAsync(TeachingPosition position, CancellationToken ct = default);
    Task<TeachingPosition> UpdateAsync(TeachingPosition position, CancellationToken ct = default);
    Task DeleteAsync(TeachingPosition position, CancellationToken ct = default);
}
