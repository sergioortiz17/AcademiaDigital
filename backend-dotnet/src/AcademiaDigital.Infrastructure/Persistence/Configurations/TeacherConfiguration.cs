using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("academic_teacher");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(t => t.EmployeeNumber).HasColumnName("employee_number").HasMaxLength(50).IsRequired();
        builder.HasIndex(t => t.EmployeeNumber).IsUnique();
        builder.Property(t => t.Department).HasColumnName("department").HasMaxLength(200);
        builder.Property(t => t.SpecializationArea).HasColumnName("specialization_area").HasMaxLength(200);
        builder.Property(t => t.HireDate).HasColumnName("hire_date");
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.UserId).HasColumnName("user_id");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
