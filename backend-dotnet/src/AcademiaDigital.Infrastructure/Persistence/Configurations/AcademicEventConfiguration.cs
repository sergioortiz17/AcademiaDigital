using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class AcademicEventConfiguration : IEntityTypeConfiguration<AcademicEvent>
{
    public void Configure(EntityTypeBuilder<AcademicEvent> builder)
    {
        builder.ToTable("AcademicEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Scope).HasConversion<int>().HasDefaultValue(Domain.Enums.EventScope.Global);
        builder.Property(e => e.Modality).HasMaxLength(20);
        builder.Property(e => e.IsPublished).HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CourseSection)
            .WithMany()
            .HasForeignKey(e => e.CourseSectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
