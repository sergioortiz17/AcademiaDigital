using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class AdmissionApplicationStatusHistoryConfiguration
    : IEntityTypeConfiguration<AdmissionApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<AdmissionApplicationStatusHistory> builder)
    {
        builder.ToTable("AdmissionApplicationStatusHistory");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(history => history.AdmissionApplicationId).HasColumnName("admission_application_id").IsRequired();
        builder.Property(history => history.FromStatus).HasColumnName("from_status").HasConversion<int?>();
        builder.Property(history => history.ToStatus).HasColumnName("to_status").HasConversion<int>().IsRequired();
        builder.Property(history => history.ChangedAt).HasColumnName("changed_at").IsRequired();
        builder.Property(history => history.ChangedByUserId).HasColumnName("changed_by_user_id");
        builder.Property(history => history.Reason).HasColumnName("reason").HasMaxLength(500);

        builder.HasIndex(history => new { history.AdmissionApplicationId, history.ChangedAt });
        builder.HasOne(history => history.AdmissionApplication)
            .WithMany(application => application.StatusHistory)
            .HasForeignKey(history => history.AdmissionApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(history => history.ChangedByUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
