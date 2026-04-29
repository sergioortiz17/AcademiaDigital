using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class ContestApplicationConfiguration : IEntityTypeConfiguration<ContestApplication>
{
    public void Configure(EntityTypeBuilder<ContestApplication> builder)
    {
        builder.ToTable("ContestApplications");

        builder.HasKey(ca => ca.Id);
        builder.Property(ca => ca.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(ca => ca.ApplicationDate).HasColumnName("application_date");
        builder.Property(ca => ca.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(ca => ca.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(ca => ca.ContestId).HasColumnName("contest_id");
        builder.Property(ca => ca.ApplicantId).HasColumnName("applicant_id");

        // Un usuario no puede postularse dos veces al mismo concurso
        builder.HasIndex(ca => new { ca.ContestId, ca.ApplicantId }).IsUnique();

        builder.HasOne(ca => ca.Contest)
            .WithMany(tc => tc.Applications)
            .HasForeignKey(ca => ca.ContestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ca => ca.Applicant)
            .WithMany()
            .HasForeignKey(ca => ca.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
