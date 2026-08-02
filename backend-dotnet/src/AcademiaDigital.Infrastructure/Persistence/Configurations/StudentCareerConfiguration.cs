using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class StudentCareerConfiguration : IEntityTypeConfiguration<StudentCareer>
{
    public void Configure(EntityTypeBuilder<StudentCareer> builder)
    {
        builder.ToTable("StudentCareers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.EnrollmentDate).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => new { x.StudentId, x.CareerId }).IsUnique();
        builder.HasIndex(x => new { x.CareerId, x.IsActive });
        builder.HasOne(x => x.Student).WithMany(x => x.Careers).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Career).WithMany(x => x.StudentCareers).HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Restrict);
    }
}
