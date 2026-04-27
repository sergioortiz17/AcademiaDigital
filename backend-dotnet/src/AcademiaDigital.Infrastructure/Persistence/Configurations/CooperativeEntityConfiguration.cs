using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CooperativeEntityConfiguration : IEntityTypeConfiguration<CooperativeEntity>
{
    public void Configure(EntityTypeBuilder<CooperativeEntity> builder)
    {
        builder.ToTable("academic_cooperative_entity");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
        builder.Property(e => e.Cuit).HasColumnName("cuit").HasMaxLength(13).IsRequired();
        builder.HasIndex(e => e.Cuit).IsUnique();
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(e => e.ContactPerson).HasColumnName("contact_person").HasMaxLength(200);
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.JoinDate).HasColumnName("join_date");
    }
}
