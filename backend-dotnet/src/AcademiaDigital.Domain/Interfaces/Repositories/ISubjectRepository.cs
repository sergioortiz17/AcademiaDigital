using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ISubjectRepository
{
    Task<IEnumerable<Subject>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Subject>> GetByCareerAsync(int careerId, CancellationToken ct = default);
    Task<Subject?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<Subject?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<Subject>> GetPrerequisitesAsync(int subjectId, CancellationToken ct = default);
    Task AddPrerequisiteAsync(int subjectId, int prerequisiteSubjectId, CancellationToken ct = default);
    Task RemovePrerequisiteAsync(int subjectId, int prerequisiteSubjectId, CancellationToken ct = default);
    Task<Subject> CreateAsync(Subject subject, CancellationToken ct = default);
    Task<Subject> UpdateAsync(Subject subject, CancellationToken ct = default);
    Task DeleteAsync(Subject subject, CancellationToken ct = default);
}
