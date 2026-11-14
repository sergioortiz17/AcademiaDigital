using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Finance.Infrastructure.Persistence;

public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
{
    public DbSet<FinancialConcept> FinancialConcepts => Set<FinancialConcept>();
    public DbSet<FinancialRate> FinancialRates => Set<FinancialRate>();
    public DbSet<FinancialBenefit> FinancialBenefits => Set<FinancialBenefit>();
    public DbSet<BillingPlan> BillingPlans => Set<BillingPlan>();
    public DbSet<BillingPlanItem> BillingPlanItems => Set<BillingPlanItem>();
    public DbSet<DebtGenerationBatch> DebtGenerationBatches => Set<DebtGenerationBatch>();
    public DbSet<StudentDebt> StudentDebts => Set<StudentDebt>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<PaymentReconciliation> PaymentReconciliations => Set<PaymentReconciliation>();
    public DbSet<PaymentReversal> PaymentReversals => Set<PaymentReversal>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptSequence> ReceiptSequences => Set<ReceiptSequence>();
    public DbSet<AcademicStudentSnapshot> AcademicStudentSnapshots => Set<AcademicStudentSnapshot>();
    public DbSet<DelinquencyAlertRule> DelinquencyAlertRules => Set<DelinquencyAlertRule>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<ExpenseEntry> ExpenseEntries => Set<ExpenseEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("finance");

        modelBuilder.ApplyConfiguration(new FinancialConceptConfiguration());
        modelBuilder.ApplyConfiguration(new FinancialRateConfiguration());
        modelBuilder.ApplyConfiguration(new FinancialBenefitConfiguration());
        modelBuilder.ApplyConfiguration(new BillingPlanConfiguration());
        modelBuilder.ApplyConfiguration(new BillingPlanItemConfiguration());
        modelBuilder.ApplyConfiguration(new DebtGenerationBatchConfiguration());
        modelBuilder.ApplyConfiguration(new StudentDebtConfiguration());

        modelBuilder.ApplyConfiguration(new PaymentMethodConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentReconciliationConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentReversalConfiguration());

        modelBuilder.ApplyConfiguration(new ReceiptConfiguration());
        modelBuilder.ApplyConfiguration(new ReceiptSequenceConfiguration());
        modelBuilder.ApplyConfiguration(new AcademicStudentSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new DelinquencyAlertRuleConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseEntryConfiguration());
    }
}
