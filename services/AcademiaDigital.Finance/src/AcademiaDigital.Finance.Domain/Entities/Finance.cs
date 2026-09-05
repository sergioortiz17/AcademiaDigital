using System.Text.Json.Serialization;

namespace AcademiaDigital.Finance.Domain.Entities;

// StudentStatus is a shared *value* (not an entity). The Student entity stays in the
// monolith, but the debt/rate/benefit records need to remember the condition the
// calculation was made for, so the enum is copied into the Finance domain by design.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudentStatus
{
    Regular = 0,
    Libre = 1,
    Graduated = 2,
    Withdrawn = 3
}

public sealed class FinancialConcept
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class FinancialRate
{
    public long Id { get; set; }
    public int FinancialConceptId { get; set; }
    public FinancialConcept FinancialConcept { get; set; } = null!;
    // Career navigation cut on extraction — Career lives in the monolith. CareerId stays.
    public int CareerId { get; set; }
    public int AcademicYear { get; set; }
    public StudentStatus? StudentCondition { get; set; }
    public decimal Amount { get; set; }
    public decimal SurchargePercentage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FinancialBenefitKind
{
    Discount = 0,
    Scholarship = 1
}

public sealed class FinancialBenefit
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FinancialBenefitKind Kind { get; set; }
    // Scholarship navigation cut on extraction — ScholarshipId stays as an opaque id.
    public int? ScholarshipId { get; set; }
    // Career navigation cut on extraction — CareerId stays.
    public int? CareerId { get; set; }
    public StudentStatus? StudentCondition { get; set; }
    public decimal Percentage { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class BillingPlan
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Career navigation cut on extraction — CareerId stays.
    public int CareerId { get; set; }
    public int AcademicYear { get; set; }
    public string Currency { get; set; } = "ARS";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    // CreatedByUser navigation cut on extraction — CreatedByUserId stays.
    public long CreatedByUserId { get; set; }
    public ICollection<BillingPlanItem> Items { get; set; } = [];
}

public sealed class BillingPlanItem
{
    public long Id { get; set; }
    public long BillingPlanId { get; set; }
    public BillingPlan BillingPlan { get; set; } = null!;
    public int FinancialConceptId { get; set; }
    public FinancialConcept FinancialConcept { get; set; } = null!;
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
}

public sealed class DebtGenerationBatch
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string IdempotencyKey { get; set; } = string.Empty;
    public long BillingPlanId { get; set; }
    public BillingPlan BillingPlan { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
    // GeneratedByUser navigation cut on extraction — GeneratedByUserId stays.
    public long GeneratedByUserId { get; set; }
    public int GeneratedDebtCount { get; set; }
    public decimal GeneratedTotal { get; set; }
    public ICollection<StudentDebt> Debts { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudentDebtStatus
{
    Pending = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Cancelled = 3
}

public sealed class StudentDebt
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long DebtGenerationBatchId { get; set; }
    public DebtGenerationBatch DebtGenerationBatch { get; set; } = null!;
    public long BillingPlanItemId { get; set; }
    public BillingPlanItem BillingPlanItem { get; set; } = null!;
    // Student / StudentCareer navigations cut on extraction — ids stay as opaque ids.
    public long StudentId { get; set; }
    public long StudentCareerId { get; set; }
    public int FinancialConceptId { get; set; }
    public FinancialConcept FinancialConcept { get; set; } = null!;
    public string Currency { get; set; } = "ARS";
    public DateOnly DueDate { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public StudentDebtStatus Status { get; set; } = StudentDebtStatus.Pending;
    public long FinancialRateId { get; set; }
    public FinancialRate FinancialRate { get; set; } = null!;
    public long? AppliedBenefitId { get; set; }
    public FinancialBenefit? AppliedBenefit { get; set; }
    public string CalculationSnapshotJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
