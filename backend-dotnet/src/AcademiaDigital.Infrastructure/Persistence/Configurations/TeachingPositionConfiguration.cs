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
        builder.Property(tp => tp.SubjectId).HasColumnName("subject_id");
        builder.Property(tp => tp.TeacherId).HasColumnName("teacher_id");

        builder.HasOne(tp => tp.Subject)
            .WithMany()
            .HasForeignKey(tp => tp.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tp => tp.Teacher)
            .WithMany()
            .HasForeignKey(tp => tp.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
