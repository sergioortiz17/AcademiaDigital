using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CertificateRequestConfiguration : IEntityTypeConfiguration<CertificateRequest>
{
    public void Configure(EntityTypeBuilder<CertificateRequest> builder)
    {
        builder.ToTable("CertificateRequests");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.CertificateType).HasColumnName("certificate_type").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasDefaultValue(CertificateStatus.Pending);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired(false);

        builder.HasOne(c => c.User)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
