using AcademiaDigital.Finance.Domain.Entities;

namespace AcademiaDigital.Finance.Domain.Interfaces.Repositories;

public sealed record DelinquencyFilter(int? CareerId, int? CommissionId, int? AcademicYear, long? StudentId);

public interface IDelinquencyRepository
{
    Task<IReadOnlyList<StudentDebt>> GetOpenDebtsAsync(DelinquencyFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicStudentSnapshot>> GetStudentSnapshotsAsync(DelinquencyFilter filter, CancellationToken ct = default);
    Task<AcademicStudentSnapshot?> FindSnapshotForUpdateAsync(long studentCareerId, CancellationToken ct = default);
    void AddSnapshot(AcademicStudentSnapshot snapshot);
    Task<IReadOnlyList<DelinquencyAlertRule>> GetRulesAsync(CancellationToken ct = default);
    Task<DelinquencyAlertRule?> FindRuleAsync(long id, bool tracking, CancellationToken ct = default);
    void AddRule(DelinquencyAlertRule rule);
}
