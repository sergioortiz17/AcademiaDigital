using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class TeacherDocumentConfiguration : IEntityTypeConfiguration<TeacherDocument>
{
    public void Configure(EntityTypeBuilder<TeacherDocument> builder)
    {
        builder.ToTable("TeacherDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(document => document.TeacherId).HasColumnName("teacher_id");
        builder.Property(document => document.DocumentType).HasColumnName("document_type").HasMaxLength(50).IsRequired();
        builder.Property(document => document.Version).HasColumnName("version");
        builder.Property(document => document.FileUrl).HasColumnName("file_url").HasMaxLength(1000).IsRequired();
        builder.Property(document => document.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(document => document.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(document => document.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(document => document.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(document => document.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(document => document.ValidUntil).HasColumnName("valid_until");
        builder.Property(document => document.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(document => document.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(document => document.Observation).HasColumnName("observation").HasMaxLength(1000);

        builder.HasIndex(document => new { document.TeacherId, document.DocumentType, document.Version }).IsUnique();
        builder.HasIndex(document => new { document.TeacherId, document.SubmittedAt });
        builder.HasOne(document => document.Teacher).WithMany().HasForeignKey(document => document.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(document => document.ReviewedByUser).WithMany().HasForeignKey(document => document.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
