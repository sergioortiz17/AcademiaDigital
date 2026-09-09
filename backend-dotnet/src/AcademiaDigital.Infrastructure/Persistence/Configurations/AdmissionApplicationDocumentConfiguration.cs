using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class AdmissionApplicationDocumentConfiguration : IEntityTypeConfiguration<AdmissionApplicationDocument>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationDocument> builder)
    {
        builder.ToTable("AdmissionApplicationDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(document => document.AdmissionApplicationId).HasColumnName("admission_application_id");
        builder.Property(document => document.DocumentRequirementId).HasColumnName("document_requirement_id");
        builder.Property(document => document.FileUrl).HasColumnName("file_url").HasMaxLength(1000).IsRequired();
        builder.Property(document => document.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(document => document.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(document => document.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(document => document.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(document => document.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(document => document.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(document => document.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(document => document.Observation).HasColumnName("observation").HasMaxLength(1000);

        builder.HasIndex(document => new
        {
            document.AdmissionApplicationId,
            document.DocumentRequirementId,
            document.SubmittedAt
        });
        builder.HasOne(document => document.AdmissionApplication)
            .WithMany(application => application.Documents)
            .HasForeignKey(document => document.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(document => document.DocumentRequirement)
            .WithMany()
            .HasForeignKey(document => document.DocumentRequirementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(document => document.ReviewedByUser)
            .WithMany()
            .HasForeignKey(document => document.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
