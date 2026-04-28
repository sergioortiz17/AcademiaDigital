using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class SubjectPrerequisiteConfiguration : IEntityTypeConfiguration<SubjectPrerequisite>
{
    public void Configure(EntityTypeBuilder<SubjectPrerequisite> builder)
    {
        builder.ToTable("SubjectPrerequisites");

        builder.HasKey(sp => new { sp.SubjectId, sp.PrerequisiteSubjectId });
        builder.Property(sp => sp.SubjectId).HasColumnName("subject_id");
        builder.Property(sp => sp.PrerequisiteSubjectId).HasColumnName("prerequisite_subject_id");

        builder.HasOne(sp => sp.Subject)
            .WithMany(s => s.Prerequisites)
            .HasForeignKey(sp => sp.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.PrerequisiteSubject)
            .WithMany(s => s.IsPrerequisiteFor)
            .HasForeignKey(sp => sp.PrerequisiteSubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
