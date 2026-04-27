using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ITeacherContestRepository
{
    Task<IEnumerable<TeacherContest>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TeacherContest>> GetByStatusAsync(ContestStatus status, CancellationToken ct = default);
    Task<TeacherContest?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<TeacherContest> CreateAsync(TeacherContest contest, CancellationToken ct = default);
    Task<TeacherContest> UpdateAsync(TeacherContest contest, CancellationToken ct = default);
    Task DeleteAsync(TeacherContest contest, CancellationToken ct = default);
}
