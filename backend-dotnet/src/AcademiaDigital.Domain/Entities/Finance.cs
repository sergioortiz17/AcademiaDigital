using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

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
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
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
    public int? ScholarshipId { get; set; }
    public Scholarship? Scholarship { get; set; }
    public int? CareerId { get; set; }
    public Career? Career { get; set; }
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
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public int AcademicYear { get; set; }
    public string Currency { get; set; } = "ARS";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
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
    public long GeneratedByUserId { get; set; }
    public User GeneratedByUser { get; set; } = null!;
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
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public long StudentCareerId { get; set; }
    public StudentCareer StudentCareer { get; set; } = null!;
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
