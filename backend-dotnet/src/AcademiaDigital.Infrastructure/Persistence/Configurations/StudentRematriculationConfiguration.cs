using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class StudentRematriculationConfiguration : IEntityTypeConfiguration<StudentRematriculation>
{
    public void Configure(EntityTypeBuilder<StudentRematriculation> builder)
    {
        builder.ToTable("StudentRematriculations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.StudentId).HasColumnName("student_id");
        builder.Property(item => item.StudentCareerId).HasColumnName("student_career_id");
        builder.Property(item => item.CareerId).HasColumnName("career_id");
        builder.Property(item => item.StudyPlanId).HasColumnName("study_plan_id");
        builder.Property(item => item.CommissionId).HasColumnName("commission_id");
        builder.Property(item => item.AcademicYear).HasColumnName("academic_year");
        builder.Property(item => item.YearNumber).HasColumnName("year_number");
        builder.Property(item => item.RematriculatedAt).HasColumnName("rematriculated_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.HasIndex(item => new { item.StudentCareerId, item.AcademicYear }).IsUnique();
        builder.HasIndex(item => new { item.CareerId, item.AcademicYear });
        builder.HasOne(item => item.Student).WithMany().HasForeignKey(item => item.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.StudentCareer).WithMany(item => item.Rematriculations)
            .HasForeignKey(item => item.StudentCareerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Career).WithMany().HasForeignKey(item => item.CareerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.StudyPlan).WithMany().HasForeignKey(item => item.StudyPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Commission).WithMany().HasForeignKey(item => item.CommissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
