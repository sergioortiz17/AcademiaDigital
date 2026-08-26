using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class AdmissionAgreementConfiguration : IEntityTypeConfiguration<AdmissionAgreement>
{
    public void Configure(EntityTypeBuilder<AdmissionAgreement> builder)
    {
        builder.ToTable("AdmissionAgreements");
        builder.HasKey(agreement => agreement.Id);
        builder.Property(agreement => agreement.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(agreement => agreement.AdmissionApplicationId).HasColumnName("admission_application_id");
        builder.Property(agreement => agreement.AgreementNumber).HasColumnName("agreement_number").HasMaxLength(80).IsRequired();
        builder.Property(agreement => agreement.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(agreement => agreement.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(agreement => agreement.StorageKey).HasColumnName("storage_key").HasMaxLength(500);
        builder.Property(agreement => agreement.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(agreement => agreement.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(agreement => agreement.Sha256).HasColumnName("sha256").HasMaxLength(64);
        builder.Property(agreement => agreement.CreatedAt).HasColumnName("created_at");
        builder.Property(agreement => agreement.GeneratedAt).HasColumnName("generated_at");
        builder.Property(agreement => agreement.LastError).HasColumnName("last_error").HasMaxLength(2000);

        builder.HasIndex(agreement => agreement.AdmissionApplicationId).IsUnique();
        builder.HasIndex(agreement => agreement.AgreementNumber).IsUnique();
        builder.HasOne(agreement => agreement.AdmissionApplication)
            .WithOne(application => application.Agreement)
            .HasForeignKey<AdmissionAgreement>(agreement => agreement.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
