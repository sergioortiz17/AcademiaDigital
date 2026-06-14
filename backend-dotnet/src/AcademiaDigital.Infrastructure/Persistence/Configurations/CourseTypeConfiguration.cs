using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CourseTypeConfiguration : IEntityTypeConfiguration<CourseType>
{
    public void Configure(EntityTypeBuilder<CourseType> builder)
    {
        builder.ToTable("CourseTypes");

        builder.HasKey(ct => ct.Id);
        builder.Property(ct => ct.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(ct => ct.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(ct => ct.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(ct => ct.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(ct => ct.Code).IsUnique();
    }
}
