using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class StudentStudyPlanConfiguration : IEntityTypeConfiguration<StudentStudyPlan>
{
    public void Configure(EntityTypeBuilder<StudentStudyPlan> builder)
    {
        builder.ToTable("StudentStudyPlans");

        builder.HasKey(ssp => ssp.Id);
        builder.Property(ssp => ssp.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(ssp => ssp.StudentId).HasColumnName("student_id");
        builder.Property(ssp => ssp.StudyPlanId).HasColumnName("study_plan_id");
        builder.Property(ssp => ssp.IsCurrent).HasColumnName("is_current").HasDefaultValue(true);
        builder.Property(ssp => ssp.AssignedAt).HasColumnName("assigned_at");
        builder.Property(ssp => ssp.EndedAt).HasColumnName("ended_at");
        builder.Property(ssp => ssp.MigrationReason).HasColumnName("migration_reason").HasMaxLength(500);

        builder.HasIndex(ssp => ssp.StudentId)
            .IsUnique()
            .HasFilter("[is_current] = 1");

        builder.HasOne(ssp => ssp.Student)
            .WithMany()
            .HasForeignKey(ssp => ssp.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ssp => ssp.StudyPlan)
            .WithMany(sp => sp.StudentStudyPlans)
            .HasForeignKey(ssp => ssp.StudyPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
