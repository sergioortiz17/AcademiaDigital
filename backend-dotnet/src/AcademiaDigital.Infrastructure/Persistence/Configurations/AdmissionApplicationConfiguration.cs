using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class AdmissionApplicationConfiguration : IEntityTypeConfiguration<AdmissionApplication>
{
    public void Configure(EntityTypeBuilder<AdmissionApplication> builder)
    {
        builder.ToTable("AdmissionApplications");
        builder.HasKey(application => application.Id);
        builder.Property(application => application.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(application => application.PublicId).HasColumnName("public_id").IsRequired();
        builder.Property(application => application.AdmissionFormId).HasColumnName("admission_form_id").IsRequired();
        builder.Property(application => application.ApplicantEmail).HasColumnName("applicant_email").HasMaxLength(254).IsRequired();
        builder.Property(application => application.ApplicantDni).HasColumnName("applicant_dni").HasMaxLength(20).IsRequired();
        builder.Property(application => application.SubmittedFieldsJson).HasColumnName("submitted_fields_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(application => application.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(application => application.TermsAcceptedAt).HasColumnName("terms_accepted_at");
        builder.Property(application => application.ReservationExpiresAt).HasColumnName("reservation_expires_at");
        builder.Property(application => application.CreatedAt).HasColumnName("created_at");
        builder.Property(application => application.UpdatedAt).HasColumnName("updated_at");
        builder.Property(application => application.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(application => application.PublicId).IsUnique();
        builder.HasIndex(application => new { application.AdmissionFormId, application.ApplicantEmail }).IsUnique();
        builder.HasIndex(application => new { application.AdmissionFormId, application.ApplicantDni }).IsUnique();
        builder.HasIndex(application => new { application.Status, application.ReservationExpiresAt });
        builder.HasIndex(application => new { application.AdmissionFormId, application.Status, application.CreatedAt });
        builder.HasOne(application => application.AdmissionForm)
            .WithMany(form => form.Applications)
            .HasForeignKey(application => application.AdmissionFormId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
