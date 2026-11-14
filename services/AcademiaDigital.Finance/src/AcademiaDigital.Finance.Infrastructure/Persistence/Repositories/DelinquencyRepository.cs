using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Finance.Infrastructure.Persistence.Repositories;

public sealed class DelinquencyRepository(FinanceDbContext db) : IDelinquencyRepository
{
    public async Task<IReadOnlyList<StudentDebt>> GetOpenDebtsAsync(DelinquencyFilter filter, CancellationToken ct = default)
    {
        var query = db.StudentDebts.AsNoTracking()
            .Include(item => item.FinancialConcept)
            .Include(item => item.BillingPlanItem).ThenInclude(item => item.BillingPlan)
            .Where(item => item.Status == StudentDebtStatus.Pending || item.Status == StudentDebtStatus.PartiallyPaid);
        if (filter.CareerId.HasValue)
            query = query.Where(item => item.BillingPlanItem.BillingPlan.CareerId == filter.CareerId.Value);
        if (filter.AcademicYear.HasValue)
            query = query.Where(item => item.BillingPlanItem.BillingPlan.AcademicYear == filter.AcademicYear.Value);
        if (filter.StudentId.HasValue)
            query = query.Where(item => item.StudentId == filter.StudentId.Value);
        if (filter.CommissionId.HasValue)
        {
            var careerIds = db.AcademicStudentSnapshots
                .Where(item => item.CommissionId == filter.CommissionId.Value)
                .Select(item => item.StudentCareerId);
            query = query.Where(item => careerIds.Contains(item.StudentCareerId));
        }
        return await query.OrderBy(item => item.DueDate).ThenBy(item => item.StudentId).ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<AcademicStudentSnapshot>> GetStudentSnapshotsAsync(DelinquencyFilter filter, CancellationToken ct = default)
    {
        var query = db.AcademicStudentSnapshots.AsNoTracking().AsQueryable();
        if (filter.CareerId.HasValue) query = query.Where(item => item.CareerId == filter.CareerId.Value);
        if (filter.CommissionId.HasValue) query = query.Where(item => item.CommissionId == filter.CommissionId.Value);
        if (filter.AcademicYear.HasValue) query = query.Where(item => item.AcademicYear == filter.AcademicYear.Value);
        if (filter.StudentId.HasValue) query = query.Where(item => item.StudentId == filter.StudentId.Value);
        return await query.ToArrayAsync(ct);
    }

    public Task<AcademicStudentSnapshot?> FindSnapshotForUpdateAsync(long studentCareerId, CancellationToken ct = default)
        => db.AcademicStudentSnapshots.SingleOrDefaultAsync(item => item.StudentCareerId == studentCareerId, ct);

    public void AddSnapshot(AcademicStudentSnapshot snapshot) => db.AcademicStudentSnapshots.Add(snapshot);

    public async Task<IReadOnlyList<DelinquencyAlertRule>> GetRulesAsync(CancellationToken ct = default)
        => await db.DelinquencyAlertRules.AsNoTracking().OrderBy(item => item.MinimumDaysOverdue).ThenBy(item => item.MinimumOutstandingAmount).ToArrayAsync(ct);

    public Task<DelinquencyAlertRule?> FindRuleAsync(long id, bool tracking, CancellationToken ct = default)
    {
        var query = db.DelinquencyAlertRules.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.Id == id, ct);
    }

    public void AddRule(DelinquencyAlertRule rule) => db.DelinquencyAlertRules.Add(rule);
}
