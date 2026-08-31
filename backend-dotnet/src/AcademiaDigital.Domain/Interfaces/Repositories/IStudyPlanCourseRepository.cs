using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudyPlanCourseRepository
{
    Task<StudyPlanCourse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCourse>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCourse>> GetByStudyPlanIdAsync(int studyPlanId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int studyPlanId, int courseId, CancellationToken ct = default);
    Task<StudyPlanCourse> CreateAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default);
    Task<StudyPlanCourse> UpdateAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default);
    Task DeleteAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default);
    Task DeleteByStudyPlanIdsAsync(IReadOnlyList<int> studyPlanIds, CancellationToken ct = default);
}
