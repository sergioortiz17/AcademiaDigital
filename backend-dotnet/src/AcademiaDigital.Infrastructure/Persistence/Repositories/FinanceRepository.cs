using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class FinanceRepository(AppDbContext db) : IFinanceRepository
{
    public async Task<IReadOnlyList<FinancialConcept>> GetConceptsAsync(CancellationToken ct = default)
        => await db.FinancialConcepts.AsNoTracking().OrderBy(item => item.Code).ToArrayAsync(ct);

    public Task<FinancialConcept?> FindConceptAsync(int id, bool tracking, CancellationToken ct = default)
    {
        var query = db.FinancialConcepts.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.Id == id, ct);
    }

    public Task<bool> ConceptCodeExistsAsync(string code, int? excludingId = null, CancellationToken ct = default)
        => db.FinancialConcepts.AnyAsync(item => item.Code == code && (!excludingId.HasValue || item.Id != excludingId.Value), ct);

    public void AddConcept(FinancialConcept concept) => db.FinancialConcepts.Add(concept);

    public async Task<IReadOnlyList<FinancialRate>> GetRatesAsync(int? careerId, int? academicYear, CancellationToken ct = default)
    {
        var query = db.FinancialRates.AsNoTracking().AsQueryable();
        if (careerId.HasValue) query = query.Where(item => item.CareerId == careerId.Value);
        if (academicYear.HasValue) query = query.Where(item => item.AcademicYear == academicYear.Value);
        return await query.OrderByDescending(item => item.AcademicYear).ThenBy(item => item.CareerId).ThenBy(item => item.FinancialConceptId).ToArrayAsync(ct);
    }

    public Task<FinancialRate?> FindRateAsync(long id, bool tracking, CancellationToken ct = default)
    {
        var query = db.FinancialRates.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.Id == id, ct);
    }

    public Task<bool> RateExistsAsync(int conceptId, int careerId, int academicYear, StudentStatus? condition, long? excludingId = null, CancellationToken ct = default)
        => db.FinancialRates.AnyAsync(item => item.FinancialConceptId == conceptId && item.CareerId == careerId
            && item.AcademicYear == academicYear && item.StudentCondition == condition
            && (!excludingId.HasValue || item.Id != excludingId.Value), ct);

    public void AddRate(FinancialRate rate) => db.FinancialRates.Add(rate);

    public async Task<IReadOnlyList<FinancialBenefit>> GetBenefitsAsync(CancellationToken ct = default)
        => await db.FinancialBenefits.AsNoTracking().OrderBy(item => item.Code).ToArrayAsync(ct);

    public Task<bool> BenefitCodeExistsAsync(string code, CancellationToken ct = default)
        => db.FinancialBenefits.AnyAsync(item => item.Code == code, ct);

    public void AddBenefit(FinancialBenefit benefit) => db.FinancialBenefits.Add(benefit);

    public Task<bool> CareerExistsAsync(int careerId, CancellationToken ct = default)
        => db.Careers.AsNoTracking().AnyAsync(item => item.Id == careerId && item.IsActive, ct);

    public Task<bool> ScholarshipExistsAsync(int scholarshipId, CancellationToken ct = default)
        => db.Scholarships.AsNoTracking().AnyAsync(item => item.Id == scholarshipId && item.IsActive, ct);

    public async Task<IReadOnlyList<BillingPlan>> GetPlansAsync(int? careerId, int? academicYear, CancellationToken ct = default)
    {
        var query = db.BillingPlans.AsNoTracking().Include(item => item.Items).AsQueryable();
        if (careerId.HasValue) query = query.Where(item => item.CareerId == careerId.Value);
        if (academicYear.HasValue) query = query.Where(item => item.AcademicYear == academicYear.Value);
        return await query.OrderByDescending(item => item.AcademicYear).ThenBy(item => item.Name).ToArrayAsync(ct);
    }

    public Task<BillingPlan?> FindPlanForGenerationAsync(long id, CancellationToken ct = default)
        => db.BillingPlans
            .FromSqlInterpolated($"SELECT * FROM \"BillingPlans\" WHERE \"Id\" = {id} AND is_active = true FOR UPDATE")
            .Include(item => item.Items).ThenInclude(item => item.FinancialConcept)
            .SingleOrDefaultAsync(ct);

    public Task<bool> HasDebtsForPlanAsync(long billingPlanId, CancellationToken ct = default)
        => db.StudentDebts.AsNoTracking().AnyAsync(item => item.BillingPlanItem.BillingPlanId == billingPlanId, ct);

    public Task<bool> PlanNameExistsAsync(string name, int careerId, int academicYear, CancellationToken ct = default)
        => db.BillingPlans.AnyAsync(item => item.Name == name && item.CareerId == careerId && item.AcademicYear == academicYear, ct);

    public void AddPlan(BillingPlan plan) => db.BillingPlans.Add(plan);

    public Task<DebtGenerationBatch?> FindBatchForUpdateAsync(string idempotencyKey, CancellationToken ct = default)
        => db.DebtGenerationBatches
            .FromSqlInterpolated($"SELECT * FROM \"DebtGenerationBatches\" WHERE idempotency_key = {idempotencyKey} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<FinanceStudentTarget>> GetGenerationTargetsAsync(int careerId, int academicYear, DateOnly calculationDate, CancellationToken ct = default)
    {
        var rows = await db.StudentCareers.AsNoTracking()
            .Where(item => item.CareerId == careerId && item.IsActive)
            .Select(item => new
            {
                item.StudentId,
                UserId = item.Student.UserId,
                StudentCareerId = item.Id,
                Condition = item.Student.Status,
                StudentName = (item.Student.User.Username + " " + item.Student.User.LastName).Trim(),
                Dni = item.Student.User.Dni ?? string.Empty,
                item.Student.LegajoNumber,
                CareerName = item.Career.Name
            }).ToArrayAsync(ct);
        var studentIds = rows.Select(item => item.StudentId).ToArray();
        var scholarshipRows = await db.StudentScholarships.AsNoTracking()
            .Where(item => studentIds.Contains(item.StudentId) && item.AcademicYear == academicYear
                && item.Status == StudentScholarshipStatus.Granted
                && (!item.ValidFrom.HasValue || item.ValidFrom <= calculationDate)
                && (!item.ValidTo.HasValue || item.ValidTo >= calculationDate))
            .Select(item => new { item.StudentId, item.ScholarshipId }).ToArrayAsync(ct);
        var byStudent = scholarshipRows.GroupBy(item => item.StudentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<int>)group.Select(item => item.ScholarshipId).Distinct().ToArray());
        return rows.Select(item => new FinanceStudentTarget(
            item.StudentId, item.UserId, item.StudentCareerId, item.Condition, item.StudentName,
            item.Dni, item.LegajoNumber, item.CareerName,
            byStudent.GetValueOrDefault(item.StudentId, Array.Empty<int>()))).ToArray();
    }

    public async Task<IReadOnlyList<FinancialRate>> GetGenerationRatesAsync(int careerId, int academicYear, IReadOnlyCollection<int> conceptIds, CancellationToken ct = default)
        => await db.FinancialRates.AsNoTracking()
            .Where(item => item.CareerId == careerId && item.AcademicYear == academicYear
                && item.IsActive && conceptIds.Contains(item.FinancialConceptId))
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<FinancialBenefit>> GetGenerationBenefitsAsync(int careerId, DateOnly calculationDate, CancellationToken ct = default)
        => await db.FinancialBenefits.AsNoTracking()
            .Where(item => item.IsActive && (!item.CareerId.HasValue || item.CareerId == careerId)
                && (!item.ValidFrom.HasValue || item.ValidFrom <= calculationDate)
                && (!item.ValidTo.HasValue || item.ValidTo >= calculationDate))
            .ToArrayAsync(ct);

    public void AddBatch(DebtGenerationBatch batch) => db.DebtGenerationBatches.Add(batch);

    public async Task<IReadOnlyList<StudentDebt>> GetDebtsByBatchAsync(long batchId, CancellationToken ct = default)
        => await db.StudentDebts.AsNoTracking().Where(item => item.DebtGenerationBatchId == batchId)
            .OrderBy(item => item.DueDate).ThenBy(item => item.Id).ToArrayAsync(ct);

    public async Task<IReadOnlyList<StudentDebt>> GetDebtsByUserAsync(long userId, CancellationToken ct = default)
        => await db.StudentDebts.AsNoTracking().Where(item => item.Student.UserId == userId)
            .OrderByDescending(item => item.CreatedAt).ThenBy(item => item.DueDate).ToArrayAsync(ct);

    public async Task<IReadOnlyList<StudentDebt>> GetDebtsByStudentAsync(long studentId, CancellationToken ct = default)
        => await db.StudentDebts.AsNoTracking().Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt).ThenBy(item => item.DueDate).ToArrayAsync(ct);
}
