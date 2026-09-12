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
        // Postgres no tiene un tipo "rowversion" auto-incremental como SQL Server.
        // Se usa la columna de sistema `xmin` (siempre presente en Postgres) como
        // concurrency token nativo en vez de mapear la propiedad RowVersion a una
        // columna real: la propiedad queda sin mapear (Ignore) y el shadow property
        // "xmin" cubre el optimistic concurrency check automáticamente.
        builder.Ignore(ep => ep.RowVersion);
        builder.UseXminAsConcurrencyToken();

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
