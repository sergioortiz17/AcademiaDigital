using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CertificateRequestConfiguration : IEntityTypeConfiguration<CertificateRequest>
{
    public void Configure(EntityTypeBuilder<CertificateRequest> builder)
    {
        builder.ToTable("CertificateRequests");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.CertificateType).HasColumnName("certificate_type").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Kind).HasColumnName("kind").HasConversion<int>();
        builder.Property(c => c.StudentCareerId).HasColumnName("student_career_id");
        builder.Property(c => c.ExamRegistrationId).HasColumnName("exam_registration_id");
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(CertificateStatus.Pending);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
        builder.Property(c => c.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(c => c.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(c => c.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);

        builder.HasIndex(c => new { c.UserId, c.StudentCareerId, c.Kind, c.ExamRegistrationId })
            .IsUnique()
            .HasFilter("[status] IN (0, 1, 3)");
        builder.HasIndex(c => new { c.StudentCareerId, c.Status, c.CreatedAt });

        builder.HasOne(c => c.User)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.StudentCareer)
               .WithMany()
               .HasForeignKey(c => c.StudentCareerId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ExamRegistration)
               .WithMany()
               .HasForeignKey(c => c.ExamRegistrationId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ReviewedByUser)
               .WithMany()
               .HasForeignKey(c => c.ReviewedByUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
