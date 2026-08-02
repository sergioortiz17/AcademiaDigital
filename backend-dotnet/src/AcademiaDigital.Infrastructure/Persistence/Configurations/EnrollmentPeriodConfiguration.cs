using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class EnrollmentPeriodConfiguration : IEntityTypeConfiguration<EnrollmentPeriod>
{
    public void Configure(EntityTypeBuilder<EnrollmentPeriod> builder)
    {
        builder.ToTable("EnrollmentPeriods");

        builder.HasKey(ep => ep.Id);
        builder.Property(ep => ep.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(ep => ep.CareerId).HasColumnName("career_id");
        builder.Property(ep => ep.StudyPlanId).HasColumnName("study_plan_id");
        builder.Property(ep => ep.AcademicYear).HasColumnName("academic_year");
        builder.Property(ep => ep.Semester).HasColumnName("semester");
        builder.Property(ep => ep.QuotasMorning).HasColumnName("quotas_morning").HasDefaultValue(0);
        builder.Property(ep => ep.QuotasAfternoon).HasColumnName("quotas_afternoon").HasDefaultValue(0);
        builder.Property(ep => ep.QuotasEvening).HasColumnName("quotas_evening").HasDefaultValue(0);
        builder.Property(ep => ep.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(ep => ep.StartDate).HasColumnName("start_date");
        builder.Property(ep => ep.EndDate).HasColumnName("end_date");
        builder.Property(ep => ep.CreatedAt).HasColumnName("created_at");
        builder.Property(ep => ep.UpdatedAt).HasColumnName("updated_at");
        builder.Property(ep => ep.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasOne(ep => ep.Career)
            .WithMany()
            .HasForeignKey(ep => ep.CareerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ep => ep.StudyPlan)
            .WithMany()
            .HasForeignKey(ep => ep.StudyPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ep => ep.Enrollments)
            .WithOne(e => e.EnrollmentPeriod)
            .HasForeignKey(e => e.EnrollmentPeriodId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
