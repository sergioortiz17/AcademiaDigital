using AcademiaDigital.Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Finance.Infrastructure.Persistence.Configurations;

public sealed class AcademicStudentSnapshotConfiguration : IEntityTypeConfiguration<AcademicStudentSnapshot>
{
    public void Configure(EntityTypeBuilder<AcademicStudentSnapshot> builder)
    {
        builder.ToTable("AcademicStudentSnapshots");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.StudentName).HasColumnName("student_name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Dni).HasColumnName("dni").HasMaxLength(20).IsRequired();
        builder.Property(item => item.LegajoNumber).HasColumnName("legajo_number").HasMaxLength(50).IsRequired();
        builder.Property(item => item.CareerName).HasColumnName("career_name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.CommissionName).HasColumnName("commission_name").HasMaxLength(100);
        builder.Property(item => item.SyncedAtUtc).HasColumnName("synced_at_utc");
        builder.HasIndex(item => item.StudentCareerId).IsUnique();
        builder.HasIndex(item => new { item.IsActive, item.CareerId, item.CommissionId, item.AcademicYear });
    }
}

public sealed class DelinquencyAlertRuleConfiguration : IEntityTypeConfiguration<DelinquencyAlertRule>
{
    public void Configure(EntityTypeBuilder<DelinquencyAlertRule> builder)
    {
        builder.ToTable("DelinquencyAlertRules", table =>
        {
            table.HasCheckConstraint("CK_DelinquencyAlertRules_Amount", "\"MinimumOutstandingAmount\" >= 0");
            table.HasCheckConstraint("CK_DelinquencyAlertRules_Days", "\"MinimumDaysOverdue\" >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.MinimumOutstandingAmount).HasPrecision(18, 2);
        builder.HasIndex(item => new { item.IsActive, item.MinimumDaysOverdue });
    }
}
