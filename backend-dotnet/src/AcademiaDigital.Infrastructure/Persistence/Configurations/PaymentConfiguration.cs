using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(item => item.Kind).HasColumnName("kind").HasConversion<int>();
        builder.Property(item => item.IsActive).HasColumnName("is_active");
        builder.Property(item => item.DisplayOrder).HasColumnName("display_order");
        builder.HasIndex(item => item.Code).IsUnique();
        builder.HasIndex(item => item.Kind).IsUnique();
        builder.HasData(
            new PaymentMethod { Id = 1, Code = "CASH", Name = "Efectivo", Kind = PaymentMethodKind.Cash, IsActive = true, DisplayOrder = 1 },
            new PaymentMethod { Id = 2, Code = "BANK_TRANSFER", Name = "Transferencia bancaria", Kind = PaymentMethodKind.BankTransfer, IsActive = true, DisplayOrder = 2 },
            new PaymentMethod { Id = 3, Code = "DEBIT_CARD", Name = "Tarjeta de débito", Kind = PaymentMethodKind.DebitCard, IsActive = true, DisplayOrder = 3 },
            new PaymentMethod { Id = 4, Code = "CREDIT_CARD", Name = "Tarjeta de crédito", Kind = PaymentMethodKind.CreditCard, IsActive = true, DisplayOrder = 4 });
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
        {
            table.HasCheckConstraint("CK_Payments_Amount", "amount > 0");
            table.HasCheckConstraint("CK_Payments_Currency", "currency = 'ARS'");
            table.HasCheckConstraint("CK_Payments_Status", "status >= 0 AND status <= 4");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PublicId).HasColumnName("public_id");
        builder.Property(item => item.ConfirmationIdempotencyKey).HasColumnName("confirmation_idempotency_key").HasMaxLength(100);
        builder.Property(item => item.StudentId).HasColumnName("student_id");
        builder.Property(item => item.PaymentMethodId).HasColumnName("payment_method_id");
        builder.Property(item => item.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(item => item.ExternalReference).HasColumnName("external_reference").HasMaxLength(100);
        builder.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(item => item.ConfirmationRequestedAt).HasColumnName("confirmation_requested_at");
        builder.Property(item => item.ConfirmationRequestedByUserId).HasColumnName("confirmation_requested_by_user_id");
        builder.Property(item => item.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(item => item.ConfirmedByUserId).HasColumnName("confirmed_by_user_id");
        builder.HasIndex(item => item.PublicId).IsUnique();
        builder.HasIndex(item => item.ConfirmationIdempotencyKey)
            .IsUnique()
            .HasFilter("confirmation_idempotency_key IS NOT NULL");
        builder.HasIndex(item => new { item.StudentId, item.CreatedAt });
        builder.HasIndex(item => new { item.Status, item.CreatedAt });
        builder.HasOne(item => item.Student).WithMany().HasForeignKey(item => item.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PaymentMethod).WithMany().HasForeignKey(item => item.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ConfirmationRequestedByUser).WithMany().HasForeignKey(item => item.ConfirmationRequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ConfirmedByUser).WithMany().HasForeignKey(item => item.ConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations", table => table.HasCheckConstraint("CK_PaymentAllocations_Amount", "amount > 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PaymentId).HasColumnName("payment_id");
        builder.Property(item => item.StudentDebtId).HasColumnName("student_debt_id");
        builder.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.HasIndex(item => new { item.PaymentId, item.StudentDebtId }).IsUnique();
        builder.HasIndex(item => item.StudentDebtId);
        builder.HasOne(item => item.Payment).WithMany(payment => payment.Allocations).HasForeignKey(item => item.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.StudentDebt).WithMany().HasForeignKey(item => item.StudentDebtId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentReconciliationConfiguration : IEntityTypeConfiguration<PaymentReconciliation>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliation> builder)
    {
        builder.ToTable("PaymentReconciliations", table =>
            table.HasCheckConstraint("CK_PaymentReconciliations_Decision", "decision IN (0, 1)"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PaymentId).HasColumnName("payment_id");
        builder.Property(item => item.Decision).HasColumnName("decision").HasConversion<int>();
        builder.Property(item => item.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.HasIndex(item => item.PaymentId).IsUnique();
        builder.HasOne(item => item.Payment).WithMany(payment => payment.Reconciliations).HasForeignKey(item => item.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentReversalConfiguration : IEntityTypeConfiguration<PaymentReversal>
{
    public void Configure(EntityTypeBuilder<PaymentReversal> builder)
    {
        builder.ToTable("PaymentReversals", table => table.HasCheckConstraint("CK_PaymentReversals_Amount", "amount > 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PublicId).HasColumnName("public_id");
        builder.Property(item => item.PaymentId).HasColumnName("payment_id");
        builder.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.HasIndex(item => item.PublicId).IsUnique();
        builder.HasIndex(item => item.PaymentId).IsUnique();
        builder.HasOne(item => item.Payment).WithMany(payment => payment.Reversals).HasForeignKey(item => item.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
