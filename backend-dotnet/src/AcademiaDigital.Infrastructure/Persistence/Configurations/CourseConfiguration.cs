using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.Property(c => c.CareerId).HasColumnName("career_id");

        builder.HasIndex(c => new { c.CareerId, c.Code }).IsUnique();

        builder.HasOne(c => c.Career)
            .WithMany(career => career.Courses)
            .HasForeignKey(c => c.CareerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
