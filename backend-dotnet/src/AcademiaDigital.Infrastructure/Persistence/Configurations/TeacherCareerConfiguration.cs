using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class TeacherCareerConfiguration : IEntityTypeConfiguration<TeacherCareer>
{
    public void Configure(EntityTypeBuilder<TeacherCareer> builder)
    {
        builder.ToTable("TeacherCareers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.StartedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.TeacherId, x.CareerId }).IsUnique();
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Career).WithMany().HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Restrict);
    }
}
