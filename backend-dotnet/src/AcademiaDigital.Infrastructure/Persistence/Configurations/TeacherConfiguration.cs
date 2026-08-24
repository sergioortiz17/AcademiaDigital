using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("Teachers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(t => t.EmployeeNumber).HasColumnName("employee_number").HasMaxLength(50).IsRequired();
        builder.HasIndex(t => t.EmployeeNumber).IsUnique();
        builder.Property(t => t.Department).HasColumnName("department").HasMaxLength(200);
        builder.Property(t => t.SpecializationArea).HasColumnName("specialization_area").HasMaxLength(200);
        builder.Property(t => t.HireDate).HasColumnName("hire_date");
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.PhoneNumber).HasColumnName("PhoneNumber");
        builder.Property(t => t.AddressLine).HasColumnName("address_line").HasMaxLength(255);
        builder.Property(t => t.City).HasColumnName("city").HasMaxLength(120);
        builder.Property(t => t.Province).HasColumnName("province").HasMaxLength(120);
        builder.Property(t => t.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        builder.Property(t => t.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(200);
        builder.Property(t => t.EmergencyContactRelationship).HasColumnName("emergency_contact_relationship").HasMaxLength(100);
        builder.Property(t => t.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(30);
        builder.Property(t => t.DeactivatedAt).HasColumnName("deactivated_at");
        builder.Property(t => t.DeactivatedByUserId).HasColumnName("deactivated_by_user_id");
        builder.Property(t => t.DeactivationReason).HasColumnName("deactivation_reason").HasMaxLength(500);
        builder.Property(t => t.UserId).HasColumnName("user_id");
        builder.HasIndex(t => t.UserId).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
