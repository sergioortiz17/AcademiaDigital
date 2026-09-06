using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class TeachingPositionConfiguration : IEntityTypeConfiguration<TeachingPosition>
{
    public void Configure(EntityTypeBuilder<TeachingPosition> builder)
    {
        builder.ToTable("TeachingPositions");

        builder.HasKey(tp => tp.Id);
        builder.Property(tp => tp.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(tp => tp.AcademicYear).HasColumnName("academic_year");
        builder.Property(tp => tp.Semester).HasColumnName("semester");
        builder.Property(tp => tp.PositionType).HasColumnName("position_type").HasConversion<int>();
        builder.Property(tp => tp.MaxStudents).HasColumnName("max_students");
        builder.Property(tp => tp.IsVacant).HasColumnName("is_vacant").HasDefaultValue(true);
        builder.Property(tp => tp.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(tp => tp.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(tp => tp.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(tp => tp.DeactivatedAt).HasColumnName("deactivated_at");
        builder.Property(tp => tp.DeactivatedByUserId).HasColumnName("deactivated_by_user_id");
        builder.Property(tp => tp.DeactivationReason).HasColumnName("deactivation_reason").HasMaxLength(500);
        builder.Property(tp => tp.CourseId).HasColumnName("course_id");
        builder.Property(tp => tp.CommissionId).HasColumnName("commission_id");
        builder.Property(tp => tp.TeacherId).HasColumnName("teacher_id");

        builder.HasIndex(tp => new { tp.AcademicYear, tp.Semester, tp.IsActive });
        builder.HasIndex(tp => new { tp.CommissionId, tp.CourseId });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_TeachingPositions_AssignmentState",
            "(is_vacant AND teacher_id IS NULL) OR (NOT is_vacant AND teacher_id IS NOT NULL)"));

        builder.HasOne(tp => tp.Course)
            .WithMany()
            .HasForeignKey(tp => tp.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tp => tp.Teacher)
            .WithMany()
            .HasForeignKey(tp => tp.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(tp => tp.Commission)
            .WithMany()
            .HasForeignKey(tp => tp.CommissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tp => tp.DeactivatedByUser)
            .WithMany()
            .HasForeignKey(tp => tp.DeactivatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
