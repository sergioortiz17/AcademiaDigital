using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(s => s.LegajoNumber).HasColumnName("legajo_number").HasMaxLength(50).IsRequired();
        builder.HasIndex(s => s.LegajoNumber).IsUnique();
        builder.Property(s => s.EnrollmentDate).HasColumnName("enrollment_date");
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.HasIndex(s => s.UserId).IsUnique();
        builder.Property(s => s.CareerId).HasColumnName("career_id");
        builder.Property(s => s.AddressLine).HasColumnName("address_line").HasMaxLength(300);
        builder.Property(s => s.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(s => s.Province).HasColumnName("province").HasMaxLength(100);
        builder.Property(s => s.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        builder.Property(s => s.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(200);
        builder.Property(s => s.EmergencyContactRelationship).HasColumnName("emergency_contact_relationship").HasMaxLength(100);
        builder.Property(s => s.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(30);
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Career)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CareerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
