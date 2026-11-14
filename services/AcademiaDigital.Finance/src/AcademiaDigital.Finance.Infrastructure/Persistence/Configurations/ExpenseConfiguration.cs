using AcademiaDigital.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Finance.Infrastructure.Persistence.Configurations;

public sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.HasIndex(item => item.Code).IsUnique();
    }
}

public sealed class ExpenseEntryConfiguration : IEntityTypeConfiguration<ExpenseEntry>
{
    public void Configure(EntityTypeBuilder<ExpenseEntry> builder)
    {
        builder.ToTable("ExpenseEntries", table =>
        {
            table.HasCheckConstraint("CK_ExpenseEntries_Amount", "\"Amount\" > 0");
            table.HasCheckConstraint("CK_ExpenseEntries_Currency", "\"Currency\" = 'ARS'");
            table.HasCheckConstraint("CK_ExpenseEntries_Reversal", "(\"Kind\" = 0 AND \"ReversalOfExpenseId\" IS NULL) OR (\"Kind\" = 1 AND \"ReversalOfExpenseId\" IS NOT NULL)");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PublicId).IsRequired();
        builder.Property(item => item.Kind).HasConversion<int>();
        builder.Property(item => item.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ExpenseDate).HasColumnType("date");
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(item => item.PurchaseNote).HasMaxLength(100);
        builder.Property(item => item.Observation).HasMaxLength(1000);
        builder.Property(item => item.AttachmentStorageKey).HasMaxLength(500);
        builder.Property(item => item.AttachmentFileName).HasMaxLength(255);
        builder.Property(item => item.AttachmentContentType).HasMaxLength(100);
        builder.Property(item => item.AttachmentSha256).HasMaxLength(64).IsFixedLength();
        builder.HasIndex(item => item.PublicId).IsUnique();
        builder.HasIndex(item => new { item.ExpenseDate, item.CategoryId });
        builder.HasIndex(item => item.SupplierName);
        builder.HasIndex(item => item.ReversalOfExpenseId).IsUnique().HasFilter("\"ReversalOfExpenseId\" IS NOT NULL");
        builder.HasOne(item => item.Category).WithMany().HasForeignKey(item => item.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReversalOfExpense).WithMany().HasForeignKey(item => item.ReversalOfExpenseId).OnDelete(DeleteBehavior.Restrict);
    }
}
