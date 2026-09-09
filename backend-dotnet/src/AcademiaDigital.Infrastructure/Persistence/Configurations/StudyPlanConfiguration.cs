using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("StudyPlans");

        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(sp => sp.CareerId).HasColumnName("career_id");
        builder.Property(sp => sp.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(sp => sp.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(sp => sp.VersionNumber).HasColumnName("version_number");
        builder.Property(sp => sp.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(sp => sp.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(sp => sp.EffectiveTo).HasColumnName("effective_to");
        builder.Property(sp => sp.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(sp => sp.CreatedAt).HasColumnName("created_at");
        builder.Property(sp => sp.UpdatedAt).HasColumnName("updated_at");
        // Postgres no tiene un tipo "rowversion" auto-incremental como SQL Server.
        // Se usa la columna de sistema `xmin` (siempre presente en Postgres) como
        // concurrency token nativo en vez de mapear la propiedad RowVersion a una
        // columna real: la propiedad queda sin mapear (Ignore) y el shadow property
        // "xmin" cubre el optimistic concurrency check automáticamente.
        builder.Ignore(sp => sp.RowVersion);
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(sp => new { sp.CareerId, sp.VersionNumber }).IsUnique();

        builder.HasOne(sp => sp.Career)
            .WithMany(c => c.StudyPlans)
            .HasForeignKey(sp => sp.CareerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
