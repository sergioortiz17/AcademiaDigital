using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CourseApprovalRuleConfiguration : IEntityTypeConfiguration<CourseApprovalRule>
{
    public void Configure(EntityTypeBuilder<CourseApprovalRule> builder)
    {
        builder.ToTable("CourseApprovalRules");

        builder.HasKey(car => car.Id);
        builder.Property(car => car.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(car => car.StudyPlanCourseId).HasColumnName("study_plan_course_id");
        builder.Property(car => car.MinimumRegularGrade).HasColumnName("minimum_regular_grade").HasPrecision(5, 2);
        builder.Property(car => car.MinimumPromotionGrade).HasColumnName("minimum_promotion_grade").HasPrecision(5, 2);
        builder.Property(car => car.MinimumAttendancePercentage).HasColumnName("minimum_attendance_percentage").HasPrecision(5, 2);
        builder.Property(car => car.RequiresFinalExam).HasColumnName("requires_final_exam").HasDefaultValue(true);
        builder.Property(car => car.AllowsPromotion).HasColumnName("allows_promotion").HasDefaultValue(false);
        builder.Property(car => car.PolicyJson).HasColumnName("policy_json");
        builder.Property(car => car.CreatedAt).HasColumnName("created_at");
        builder.Property(car => car.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(car => car.StudyPlanCourseId).IsUnique();

        builder.HasOne(car => car.StudyPlanCourse)
            .WithOne(spc => spc.ApprovalRule)
            .HasForeignKey<CourseApprovalRule>(car => car.StudyPlanCourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
