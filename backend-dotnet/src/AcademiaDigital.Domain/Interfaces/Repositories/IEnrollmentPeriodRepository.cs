using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IEnrollmentPeriodRepository
{
    Task<IEnumerable<EnrollmentPeriod>> GetAllAsync(CancellationToken ct = default);
    Task<EnrollmentPeriod?> GetActiveByCareerAsync(int careerId, CancellationToken ct = default);
    Task<EnrollmentPeriod?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<(int Morning, int Afternoon, int Evening)> GetEnrolledShiftCountsAsync(int periodId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, (int Morning, int Afternoon, int Evening)>> GetAllEnrolledShiftCountsAsync(IEnumerable<int> periodIds, CancellationToken ct = default);
    Task<EnrollmentPeriod> CreateAsync(EnrollmentPeriod period, CancellationToken ct = default);
    Task<EnrollmentPeriod> UpdateAsync(EnrollmentPeriod period, CancellationToken ct = default);
    Task DeleteAsync(int periodId, CancellationToken ct = default);
}
