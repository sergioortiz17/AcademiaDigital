using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.AcademicYear).HasColumnName("academic_year");
        builder.Property(e => e.Semester).HasColumnName("semester");
        builder.Property(e => e.EnrollmentDate).HasColumnName("enrollment_date");
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(e => e.FinalGrade).HasColumnName("final_grade").HasPrecision(4, 2);
        builder.Property(e => e.StudentId).HasColumnName("student_id");
        builder.Property(e => e.SubjectId).HasColumnName("subject_id");
        builder.Property(e => e.TeachingPositionId).HasColumnName("teaching_position_id");

        // Un alumno no puede inscribirse dos veces a la misma materia en el mismo período
        builder.HasIndex(e => new { e.StudentId, e.SubjectId, e.AcademicYear, e.Semester }).IsUnique();

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Subject)
            .WithMany()
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TeachingPosition)
            .WithMany(tp => tp.Enrollments)
            .HasForeignKey(e => e.TeachingPositionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
