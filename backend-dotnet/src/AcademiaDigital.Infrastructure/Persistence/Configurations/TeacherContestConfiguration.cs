using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class TeacherContestConfiguration : IEntityTypeConfiguration<TeacherContest>
{
    public void Configure(EntityTypeBuilder<TeacherContest> builder)
    {
        builder.ToTable("academic_teacher_contest");

        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(tc => tc.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(tc => tc.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(tc => tc.OpenDate).HasColumnName("open_date");
        builder.Property(tc => tc.CloseDate).HasColumnName("close_date");
        builder.Property(tc => tc.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(tc => tc.SubjectId).HasColumnName("subject_id");
        builder.Property(tc => tc.CareerId).HasColumnName("career_id");

        builder.HasOne(tc => tc.Subject)
            .WithMany()
            .HasForeignKey(tc => tc.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(tc => tc.Career)
            .WithMany()
            .HasForeignKey(tc => tc.CareerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
