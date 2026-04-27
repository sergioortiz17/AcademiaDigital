using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("academic_subject");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(s => s.Credits).HasColumnName("credits");
        builder.Property(s => s.WeeklyHours).HasColumnName("weekly_hours");
        builder.Property(s => s.Year).HasColumnName("year");
        builder.Property(s => s.Semester).HasColumnName("semester");
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(s => s.CareerId).HasColumnName("career_id");

        builder.HasOne(s => s.Career)
            .WithMany(c => c.Subjects)
            .HasForeignKey(s => s.CareerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
