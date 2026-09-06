using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICourseSectionRepository
{
    Task<IReadOnlyList<CourseSection>> GetAllAsync(
        int? academicYear,
        int? semester,
        bool? isVacant,
        bool includeInactive,
        CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetByCourseAsync(int courseId, CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetByTeacherAsync(long teacherId, CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetByPeriodAsync(int year, int semester, CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetVacantAsync(CancellationToken ct = default);
    Task<CourseSection?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<CourseSection> CreateAsync(CourseSection position, CancellationToken ct = default);
    Task<CourseSection> UpdateAsync(CourseSection position, CancellationToken ct = default);
    Task DeactivateAsync(CourseSection position, CancellationToken ct = default);
}
