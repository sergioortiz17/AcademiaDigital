using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class CertificateIssuanceConfiguration : IEntityTypeConfiguration<CertificateIssuance>
{
    public void Configure(EntityTypeBuilder<CertificateIssuance> builder)
    {
        builder.ToTable("CertificateIssuances", table => table.HasCheckConstraint(
            "CK_CertificateIssuances_Sequence", "[sequence_number] > 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.PublicId).HasColumnName("public_id");
        builder.Property(item => item.CertificateRequestId).HasColumnName("certificate_request_id");
        builder.Property(item => item.SequenceNumber).HasColumnName("sequence_number");
        builder.Property(item => item.CertificateNumber).HasColumnName("certificate_number").HasMaxLength(30);
        builder.Property(item => item.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("text");
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(item => item.FileName).HasColumnName("file_name").HasMaxLength(150);
        builder.Property(item => item.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(item => item.StorageKey).HasColumnName("storage_key").HasMaxLength(500);
        builder.Property(item => item.Sha256).HasColumnName("sha256").HasMaxLength(64);
        builder.Property(item => item.LastError).HasColumnName("last_error").HasMaxLength(1000);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.GeneratedAt).HasColumnName("generated_at");
        builder.Property(item => item.IssuedByUserId).HasColumnName("issued_by_user_id");
        builder.HasIndex(item => item.PublicId).IsUnique();
        builder.HasIndex(item => item.CertificateRequestId).IsUnique();
        builder.HasIndex(item => item.SequenceNumber).IsUnique();
        builder.HasIndex(item => item.CertificateNumber).IsUnique();
        builder.HasOne(item => item.CertificateRequest).WithOne(item => item.Issuance)
            .HasForeignKey<CertificateIssuance>(item => item.CertificateRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.IssuedByUser).WithMany()
            .HasForeignKey(item => item.IssuedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CertificateSequenceConfiguration : IEntityTypeConfiguration<CertificateSequence>
{
    public void Configure(EntityTypeBuilder<CertificateSequence> builder)
    {
        builder.ToTable("CertificateSequences", table => table.HasCheckConstraint(
            "CK_CertificateSequences_Singleton", "[id] = 1 AND [last_value] >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.LastValue).HasColumnName("last_value");
        builder.HasData(new CertificateSequence { Id = 1, LastValue = 0 });
    }
}
