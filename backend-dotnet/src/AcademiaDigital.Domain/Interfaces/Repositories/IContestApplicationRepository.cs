using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IContestApplicationRepository
{
    Task<IEnumerable<ContestApplication>> GetByContestAsync(int contestId, CancellationToken ct = default);
    Task<IEnumerable<ContestApplication>> GetByApplicantAsync(long applicantId, CancellationToken ct = default);
    Task<ContestApplication?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<ContestApplication?> FindByContestAndApplicantAsync(int contestId, long applicantId, CancellationToken ct = default);
    Task<ContestApplication> CreateAsync(ContestApplication application, CancellationToken ct = default);
    Task<ContestApplication> UpdateAsync(ContestApplication application, CancellationToken ct = default);
    Task DeleteAsync(ContestApplication application, CancellationToken ct = default);
}
