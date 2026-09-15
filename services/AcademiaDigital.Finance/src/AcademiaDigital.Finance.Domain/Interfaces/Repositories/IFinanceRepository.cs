using AcademiaDigital.Finance.Domain.Entities;

namespace AcademiaDigital.Finance.Domain.Interfaces.Repositories;

public interface IFinanceRepository
{
    Task<IReadOnlyList<FinancialConcept>> GetConceptsAsync(CancellationToken ct = default);
    Task<FinancialConcept?> FindConceptAsync(int id, bool tracking, CancellationToken ct = default);
    Task<bool> ConceptCodeExistsAsync(string code, int? excludingId = null, CancellationToken ct = default);
    void AddConcept(FinancialConcept concept);

    Task<IReadOnlyList<FinancialRate>> GetRatesAsync(int? careerId, int? academicYear, CancellationToken ct = default);
    Task<FinancialRate?> FindRateAsync(long id, bool tracking, CancellationToken ct = default);
    Task<bool> RateExistsAsync(int conceptId, int careerId, int academicYear, StudentStatus? condition, long? excludingId = null, CancellationToken ct = default);
    void AddRate(FinancialRate rate);

    Task<IReadOnlyList<FinancialBenefit>> GetBenefitsAsync(CancellationToken ct = default);
    Task<bool> BenefitCodeExistsAsync(string code, CancellationToken ct = default);
    void AddBenefit(FinancialBenefit benefit);

    Task<IReadOnlyList<BillingPlan>> GetPlansAsync(int? careerId, int? academicYear, CancellationToken ct = default);
    Task<BillingPlan?> FindPlanForGenerationAsync(long id, CancellationToken ct = default);
    Task<bool> PlanNameExistsAsync(string name, int careerId, int academicYear, CancellationToken ct = default);
    void AddPlan(BillingPlan plan);

    Task<DebtGenerationBatch?> FindBatchForUpdateAsync(string idempotencyKey, CancellationToken ct = default);
    // After extraction Finance no longer owns the Student/StudentCareer/Scholarship tables,
    // so the debt-generation target (which student, condition and granted scholarships) is
    // supplied by the caller (the monolith) in the request body instead of being discovered
    // by joining local tables. See ADR 0001 / README contract POST /debts/generate.
    Task<bool> StudentCareerHasDebtsForPlanAsync(long billingPlanId, long studentCareerId, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialRate>> GetGenerationRatesAsync(int careerId, int academicYear, IReadOnlyCollection<int> conceptIds, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialBenefit>> GetGenerationBenefitsAsync(int careerId, DateOnly calculationDate, CancellationToken ct = default);
    void AddBatch(DebtGenerationBatch batch);
    Task<IReadOnlyList<StudentDebt>> GetDebtsByBatchAsync(long batchId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentDebt>> GetDebtsByStudentAsync(long studentId, CancellationToken ct = default);
}

// Caller-supplied debt-generation target. Student display data (name/dni/legajo) travels
// with the request so the generated debt snapshot can be rendered without calling back
// into the monolith. GrantedScholarshipIds lets Finance pick the applicable benefit.
public sealed record FinanceStudentTarget(
    long StudentId,
    long StudentCareerId,
    StudentStatus Condition,
    string StudentName,
    string Dni,
    string LegajoNumber,
    string CareerName,
    IReadOnlyCollection<int> GrantedScholarshipIds);
