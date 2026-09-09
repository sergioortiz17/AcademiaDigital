using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class EnrollmentStatusHistoryConfiguration : IEntityTypeConfiguration<EnrollmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<EnrollmentStatusHistory> b)
    {
        b.ToTable("EnrollmentStatusHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.PreviousStatus).HasConversion<int>();
        b.Property(x => x.NewStatus).HasConversion<int>();
        b.HasIndex(x => new { x.EnrollmentId, x.ChangedAt });
        b.HasOne(x => x.Enrollment).WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
