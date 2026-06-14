using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudyPlanRepository
{
    Task<IEnumerable<StudyPlan>> GetByCareerIdAsync(int careerId, CancellationToken ct = default);
    Task<StudyPlan?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<StudyPlan?> GetActiveByCareerIdAsync(int careerId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<StudyPlan> CreateAsync(StudyPlan studyPlan, CancellationToken ct = default);
    Task<StudyPlan> UpdateAsync(StudyPlan studyPlan, CancellationToken ct = default);
}
