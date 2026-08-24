using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ITeachingPositionRepository
{
    Task<IReadOnlyList<TeachingPosition>> GetAllAsync(
        int? academicYear,
        int? semester,
        bool? isVacant,
        bool includeInactive,
        CancellationToken ct = default);
    Task<IEnumerable<TeachingPosition>> GetByCourseAsync(int courseId, CancellationToken ct = default);
    Task<IEnumerable<TeachingPosition>> GetByTeacherAsync(long teacherId, CancellationToken ct = default);
    Task<IEnumerable<TeachingPosition>> GetByPeriodAsync(int year, int semester, CancellationToken ct = default);
    Task<IEnumerable<TeachingPosition>> GetVacantAsync(CancellationToken ct = default);
    Task<TeachingPosition?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<TeachingPosition> CreateAsync(TeachingPosition position, CancellationToken ct = default);
    Task<TeachingPosition> UpdateAsync(TeachingPosition position, CancellationToken ct = default);
    Task DeactivateAsync(TeachingPosition position, CancellationToken ct = default);
}
