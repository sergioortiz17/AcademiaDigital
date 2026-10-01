using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CareerConfiguration : IEntityTypeConfiguration<Career>
{
    public void Configure(EntityTypeBuilder<Career> builder)
    {
        builder.ToTable("Careers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(c => c.TotalCredits).HasColumnName("total_credits");
        builder.Property(c => c.DurationYears).HasColumnName("duration_years");
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        // Postgres no tiene un tipo "rowversion" auto-incremental como SQL Server.
        // Se usa la columna de sistema `xmin` (siempre presente en Postgres) como
        // concurrency token nativo en vez de mapear la propiedad RowVersion a una
        // columna real: la propiedad queda sin mapear (Ignore) y el shadow property
        // "xmin" cubre el optimistic concurrency check automáticamente.
        builder.Ignore(c => c.RowVersion);
        builder.UseXminAsConcurrencyToken();
    }
}
