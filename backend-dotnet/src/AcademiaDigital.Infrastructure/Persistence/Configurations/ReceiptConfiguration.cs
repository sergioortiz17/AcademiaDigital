using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts", table =>
        {
            table.HasCheckConstraint("CK_Receipts_Sequence", "[sequence_number] > 0");
            table.HasCheckConstraint("CK_Receipts_Status", "[status] >= 0 AND [status] <= 2");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PublicId).HasColumnName("public_id");
        builder.Property(item => item.PaymentId).HasColumnName("payment_id");
        builder.Property(item => item.SequenceNumber).HasColumnName("sequence_number");
        builder.Property(item => item.ReceiptNumber).HasColumnName("receipt_number").HasMaxLength(20).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(item => item.FileName).HasColumnName("file_name").HasMaxLength(150).IsRequired();
        builder.Property(item => item.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(item => item.StorageKey).HasColumnName("storage_key").HasMaxLength(500);
        builder.Property(item => item.Sha256).HasColumnName("sha256").HasMaxLength(64);
        builder.Property(item => item.LastError).HasColumnName("last_error").HasMaxLength(1000);
        builder.Property(item => item.FiscalCae).HasColumnName("fiscal_cae").HasMaxLength(50);
        builder.Property(item => item.FiscalQrData).HasColumnName("fiscal_qr_data").HasMaxLength(2000);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.GeneratedAt).HasColumnName("generated_at");
        builder.Property(item => item.IssuedByUserId).HasColumnName("issued_by_user_id");
        builder.HasIndex(item => item.PublicId).IsUnique();
        builder.HasIndex(item => item.PaymentId).IsUnique();
        builder.HasIndex(item => item.SequenceNumber).IsUnique();
        builder.HasIndex(item => item.ReceiptNumber).IsUnique();
        builder.HasIndex(item => new { item.Status, item.CreatedAt });
        builder.HasOne(item => item.Payment).WithOne(item => item.Receipt)
            .HasForeignKey<Receipt>(item => item.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.IssuedByUser).WithMany()
            .HasForeignKey(item => item.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReceiptSequenceConfiguration : IEntityTypeConfiguration<ReceiptSequence>
{
    public void Configure(EntityTypeBuilder<ReceiptSequence> builder)
    {
        builder.ToTable("ReceiptSequences", table => table.HasCheckConstraint(
            "CK_ReceiptSequences_Singleton", "[id] = 1 AND [last_value] >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.LastValue).HasColumnName("last_value");
        builder.HasData(new ReceiptSequence { Id = 1, LastValue = 0 });
    }
}
