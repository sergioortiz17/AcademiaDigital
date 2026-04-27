using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class AdministrativeConfiguration : IEntityTypeConfiguration<Administrative>
{
    public void Configure(EntityTypeBuilder<Administrative> builder)
    {
        builder.ToTable("academic_administrative");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(a => a.EmployeeNumber).HasColumnName("employee_number").HasMaxLength(50).IsRequired();
        builder.HasIndex(a => a.EmployeeNumber).IsUnique();
        builder.Property(a => a.Department).HasColumnName("department").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Position).HasColumnName("position").HasMaxLength(200).IsRequired();
        builder.Property(a => a.HireDate).HasColumnName("hire_date");
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(a => a.UserId).HasColumnName("user_id");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
