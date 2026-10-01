using AcademiaDigital.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Finance.Infrastructure.Persistence.Configurations;

public sealed class FinancialConceptConfiguration : IEntityTypeConfiguration<FinancialConcept>
{
    public void Configure(EntityTypeBuilder<FinancialConcept> builder)
    {
        builder.ToTable("FinancialConcepts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(item => item.IsActive).HasColumnName("is_active");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(item => item.Code).IsUnique();
    }
}

public sealed class FinancialRateConfiguration : IEntityTypeConfiguration<FinancialRate>
{
    public void Configure(EntityTypeBuilder<FinancialRate> builder)
    {
        builder.ToTable("FinancialRates", table =>
        {
            table.HasCheckConstraint("CK_FinancialRates_Amount", "amount > 0");
            table.HasCheckConstraint("CK_FinancialRates_Surcharge", "surcharge_percentage >= 0 AND surcharge_percentage <= 100");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.FinancialConceptId).HasColumnName("financial_concept_id");
        builder.Property(item => item.CareerId).HasColumnName("career_id");
        builder.Property(item => item.AcademicYear).HasColumnName("academic_year");
        builder.Property(item => item.StudentCondition).HasColumnName("student_condition").HasConversion<int?>();
        builder.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(item => item.SurchargePercentage).HasColumnName("surcharge_percentage").HasPrecision(5, 2);
        builder.Property(item => item.IsActive).HasColumnName("is_active");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(item => new { item.FinancialConceptId, item.CareerId, item.AcademicYear, item.StudentCondition })
            .IsUnique()
            .HasFilter("student_condition IS NOT NULL");
        builder.HasIndex(item => new { item.FinancialConceptId, item.CareerId, item.AcademicYear })
            .IsUnique()
            .HasDatabaseName("UX_FinancialRates_Default")
            .HasFilter("student_condition IS NULL");
        builder.HasOne(item => item.FinancialConcept).WithMany().HasForeignKey(item => item.FinancialConceptId).OnDelete(DeleteBehavior.Restrict);
        // Career relation cut on extraction — CareerId is an opaque reference (indexed only).
        builder.HasIndex(item => new { item.CareerId, item.AcademicYear });
    }
}

public sealed class FinancialBenefitConfiguration : IEntityTypeConfiguration<FinancialBenefit>
{
    public void Configure(EntityTypeBuilder<FinancialBenefit> builder)
    {
        builder.ToTable("FinancialBenefits", table =>
        {
            table.HasCheckConstraint("CK_FinancialBenefits_Percentage", "percentage > 0 AND percentage <= 100");
            table.HasCheckConstraint("CK_FinancialBenefits_Scholarship", "(kind = 0 AND scholarship_id IS NULL) OR (kind = 1 AND scholarship_id IS NOT NULL)");
            table.HasCheckConstraint("CK_FinancialBenefits_Validity", "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(item => item.Kind).HasColumnName("kind").HasConversion<int>();
        builder.Property(item => item.ScholarshipId).HasColumnName("scholarship_id");
        builder.Property(item => item.CareerId).HasColumnName("career_id");
        builder.Property(item => item.StudentCondition).HasColumnName("student_condition").HasConversion<int?>();
        builder.Property(item => item.Percentage).HasColumnName("percentage").HasPrecision(5, 2);
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from").HasColumnType("date");
        builder.Property(item => item.ValidTo).HasColumnName("valid_to").HasColumnType("date");
        builder.Property(item => item.IsActive).HasColumnName("is_active");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(item => item.Code).IsUnique();
        builder.HasIndex(item => new { item.CareerId, item.IsActive });
        // Scholarship and Career relations cut on extraction — ids are opaque references.
    }
}

public sealed class BillingPlanConfiguration : IEntityTypeConfiguration<BillingPlan>
{
    public void Configure(EntityTypeBuilder<BillingPlan> builder)
    {
        builder.ToTable("BillingPlans");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(item => item.CareerId).HasColumnName("career_id");
        builder.Property(item => item.AcademicYear).HasColumnName("academic_year");
        builder.Property(item => item.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(item => item.IsActive).HasColumnName("is_active");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.HasIndex(item => new { item.CareerId, item.AcademicYear, item.Name }).IsUnique();
        // Career and CreatedByUser relations cut on extraction — ids are opaque references.
    }
}

public sealed class BillingPlanItemConfiguration : IEntityTypeConfiguration<BillingPlanItem>
{
    public void Configure(EntityTypeBuilder<BillingPlanItem> builder)
    {
        builder.ToTable("BillingPlanItems", table => table.HasCheckConstraint("CK_BillingPlanItems_Installment", "installment_number > 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.BillingPlanId).HasColumnName("billing_plan_id");
        builder.Property(item => item.FinancialConceptId).HasColumnName("financial_concept_id");
        builder.Property(item => item.InstallmentNumber).HasColumnName("installment_number");
        builder.Property(item => item.DueDate).HasColumnName("due_date").HasColumnType("date");
        builder.HasIndex(item => new { item.BillingPlanId, item.FinancialConceptId, item.InstallmentNumber }).IsUnique();
        builder.HasIndex(item => new { item.BillingPlanId, item.DueDate });
        builder.HasOne(item => item.BillingPlan).WithMany(plan => plan.Items).HasForeignKey(item => item.BillingPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.FinancialConcept).WithMany().HasForeignKey(item => item.FinancialConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DebtGenerationBatchConfiguration : IEntityTypeConfiguration<DebtGenerationBatch>
{
    public void Configure(EntityTypeBuilder<DebtGenerationBatch> builder)
    {
        builder.ToTable("DebtGenerationBatches", table =>
        {
            table.HasCheckConstraint("CK_DebtGenerationBatches_Count", "generated_debt_count >= 0");
            table.HasCheckConstraint("CK_DebtGenerationBatches_Total", "generated_total >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PublicId).HasColumnName("public_id");
        builder.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(item => item.BillingPlanId).HasColumnName("billing_plan_id");
        builder.Property(item => item.GeneratedAt).HasColumnName("generated_at");
        builder.Property(item => item.GeneratedByUserId).HasColumnName("generated_by_user_id");
        builder.Property(item => item.GeneratedDebtCount).HasColumnName("generated_debt_count");
        builder.Property(item => item.GeneratedTotal).HasColumnName("generated_total").HasPrecision(18, 2);
        builder.HasIndex(item => item.PublicId).IsUnique();
        builder.HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.HasOne(item => item.BillingPlan).WithMany().HasForeignKey(item => item.BillingPlanId).OnDelete(DeleteBehavior.Restrict);
        // GeneratedByUser relation cut on extraction — GeneratedByUserId is an opaque reference.
    }
}

public sealed class StudentDebtConfiguration : IEntityTypeConfiguration<StudentDebt>
{
    public void Configure(EntityTypeBuilder<StudentDebt> builder)
    {
        builder.ToTable("StudentDebts", table =>
        {
            table.HasCheckConstraint("CK_StudentDebts_Amounts", "base_amount > 0 AND surcharge_amount >= 0 AND discount_amount >= 0 AND total_amount >= 0 AND paid_amount >= 0 AND paid_amount <= total_amount");
            table.HasCheckConstraint("CK_StudentDebts_Currency", "currency = 'ARS'");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PublicId).HasColumnName("public_id");
        builder.Property(item => item.DebtGenerationBatchId).HasColumnName("debt_generation_batch_id");
        builder.Property(item => item.BillingPlanItemId).HasColumnName("billing_plan_item_id");
        builder.Property(item => item.StudentId).HasColumnName("student_id");
        builder.Property(item => item.StudentCareerId).HasColumnName("student_career_id");
        builder.Property(item => item.FinancialConceptId).HasColumnName("financial_concept_id");
        builder.Property(item => item.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(item => item.DueDate).HasColumnName("due_date").HasColumnType("date");
        builder.Property(item => item.BaseAmount).HasColumnName("base_amount").HasPrecision(18, 2);
        builder.Property(item => item.SurchargeAmount).HasColumnName("surcharge_amount").HasPrecision(18, 2);
        builder.Property(item => item.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
        builder.Property(item => item.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
        builder.Property(item => item.PaidAmount).HasColumnName("paid_amount").HasPrecision(18, 2);
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(item => item.FinancialRateId).HasColumnName("financial_rate_id");
        builder.Property(item => item.AppliedBenefitId).HasColumnName("applied_benefit_id");
        builder.Property(item => item.CalculationSnapshotJson).HasColumnName("calculation_snapshot_json").HasColumnType("text").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(item => item.PublicId).IsUnique();
        builder.HasIndex(item => new { item.DebtGenerationBatchId, item.StudentCareerId, item.BillingPlanItemId }).IsUnique();
        builder.HasIndex(item => new { item.StudentCareerId, item.BillingPlanItemId }).IsUnique();
        builder.HasIndex(item => new { item.StudentId, item.Status, item.DueDate });
        builder.HasOne(item => item.DebtGenerationBatch).WithMany(batch => batch.Debts).HasForeignKey(item => item.DebtGenerationBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.BillingPlanItem).WithMany().HasForeignKey(item => item.BillingPlanItemId).OnDelete(DeleteBehavior.Restrict);
        // Student and StudentCareer relations cut on extraction — ids are opaque references.
        builder.HasOne(item => item.FinancialConcept).WithMany().HasForeignKey(item => item.FinancialConceptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.FinancialRate).WithMany().HasForeignKey(item => item.FinancialRateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.AppliedBenefit).WithMany().HasForeignKey(item => item.AppliedBenefitId).OnDelete(DeleteBehavior.Restrict);
    }
}
