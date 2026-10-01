using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICoursePrerequisiteRepository
{
    Task<IReadOnlyList<CoursePrerequisite>> GetByStudyPlanIdAsync(int studyPlanId, CancellationToken ct = default);
    Task<IReadOnlyList<CoursePrerequisite>> GetByCourseAsync(int studyPlanId, int courseId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int studyPlanId, int courseId, int prerequisiteCourseId, CancellationToken ct = default);
    Task<CoursePrerequisite> CreateAsync(CoursePrerequisite prerequisite, CancellationToken ct = default);
    Task RemoveAsync(int studyPlanId, int courseId, int prerequisiteCourseId, CancellationToken ct = default);
    Task DeleteByStudyPlanIdsAsync(IReadOnlyList<int> studyPlanIds, CancellationToken ct = default);
}
