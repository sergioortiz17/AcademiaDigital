using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {
        builder.ToTable("CoursePrerequisites");

        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(cp => cp.StudyPlanId).HasColumnName("study_plan_id");
        builder.Property(cp => cp.CourseId).HasColumnName("course_id");
        builder.Property(cp => cp.PrerequisiteCourseId).HasColumnName("prerequisite_course_id");
        builder.Property(cp => cp.PrerequisiteType).HasColumnName("prerequisite_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(cp => cp.MinimumRequiredStatus).HasColumnName("minimum_required_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(cp => cp.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(cp => cp.CreatedAt).HasColumnName("created_at");
        builder.Property(cp => cp.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(cp => new { cp.StudyPlanId, cp.CourseId, cp.PrerequisiteCourseId })
            .IsUnique()
            .HasFilter("[is_active] = 1");

        builder.ToTable(t => t.HasCheckConstraint("CK_CoursePrerequisites_NoSelfReference", "course_id <> prerequisite_course_id"));

        builder.HasOne(cp => cp.StudyPlan)
            .WithMany(sp => sp.Prerequisites)
            .HasForeignKey(cp => cp.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.Course)
            .WithMany(c => c.Prerequisites)
            .HasForeignKey(cp => cp.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cp => cp.PrerequisiteCourse)
            .WithMany(c => c.IsPrerequisiteFor)
            .HasForeignKey(cp => cp.PrerequisiteCourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
