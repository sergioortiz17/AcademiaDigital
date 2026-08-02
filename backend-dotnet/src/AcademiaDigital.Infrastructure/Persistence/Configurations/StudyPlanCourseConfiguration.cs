using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class StudyPlanCourseConfiguration : IEntityTypeConfiguration<StudyPlanCourse>
{
    public void Configure(EntityTypeBuilder<StudyPlanCourse> builder)
    {
        builder.ToTable("StudyPlanCourses");

        builder.HasKey(spc => spc.Id);
        builder.Property(spc => spc.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(spc => spc.StudyPlanId).HasColumnName("study_plan_id");
        builder.Property(spc => spc.CourseId).HasColumnName("course_id");
        builder.Property(spc => spc.YearNumber).HasColumnName("year_number");
        builder.Property(spc => spc.Semester).HasColumnName("semester");
        builder.Property(spc => spc.CourseTypeId).HasColumnName("course_type_id");
        builder.Property(spc => spc.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(spc => spc.IsMandatory).HasColumnName("is_mandatory").HasDefaultValue(true);
        builder.Property(spc => spc.Credits).HasColumnName("credits").HasPrecision(5, 2);
        builder.Property(spc => spc.WorkloadHours).HasColumnName("workload_hours");
        builder.Property(spc => spc.IsAnnual).HasColumnName("is_annual").HasDefaultValue(false);
        builder.Property(spc => spc.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(spc => spc.CreatedAt).HasColumnName("created_at");
        builder.Property(spc => spc.UpdatedAt).HasColumnName("updated_at");
        builder.Property(spc => spc.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(spc => new { spc.StudyPlanId, spc.CourseId }).IsUnique();
        builder.HasIndex(spc => new { spc.StudyPlanId, spc.YearNumber, spc.Semester, spc.SortOrder });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_StudyPlanCourses_YearNumber", "year_number > 0");
            t.HasCheckConstraint("CK_StudyPlanCourses_Semester", "semester IN (1, 2)");
        });

        builder.HasOne(spc => spc.StudyPlan)
            .WithMany(sp => sp.Courses)
            .HasForeignKey(spc => spc.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(spc => spc.Course)
            .WithMany(c => c.StudyPlanCourses)
            .HasForeignKey(spc => spc.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(spc => spc.CourseType)
            .WithMany(ct => ct.StudyPlanCourses)
            .HasForeignKey(spc => spc.CourseTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
