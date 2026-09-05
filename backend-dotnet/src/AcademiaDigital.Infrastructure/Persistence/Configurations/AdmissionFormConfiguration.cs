using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class AdmissionFormConfiguration : IEntityTypeConfiguration<AdmissionForm>
{
    public void Configure(EntityTypeBuilder<AdmissionForm> builder)
    {
        builder.ToTable("AdmissionForms", table => table.HasCheckConstraint(
            "CK_AdmissionForms_Capacity",
            "capacity IS NULL OR (capacity >= 1 AND capacity <= 100000)"));
        builder.HasKey(form => form.Id);
        builder.Property(form => form.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(form => form.CareerId).HasColumnName("career_id").IsRequired();
        builder.Property(form => form.CommissionId).HasColumnName("commission_id");
        builder.Property(form => form.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        builder.Property(form => form.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(form => form.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(form => form.TermsText).HasColumnName("terms_text").HasMaxLength(8000).IsRequired();
        builder.Property(form => form.ReservationHours).HasColumnName("reservation_hours").HasDefaultValue(72);
        builder.Property(form => form.Capacity).HasColumnName("capacity");
        builder.Property(form => form.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(form => form.CreatedAt).HasColumnName("created_at");
        builder.Property(form => form.UpdatedAt).HasColumnName("updated_at");
        // Postgres: se usa la columna de sistema xmin como concurrency token en vez de
        // mapear RowVersion (byte[], tipo rowversion de SQL Server que Npgsql no soporta).
        builder.Ignore(form => form.RowVersion);
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(form => form.Slug).IsUnique();
        builder.HasIndex(form => form.CommissionId)
            .IsUnique()
            .HasFilter("commission_id IS NOT NULL");
        builder.HasOne(form => form.Career)
            .WithMany()
            .HasForeignKey(form => form.CareerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(form => form.Commission)
            .WithMany()
            .HasForeignKey(form => form.CommissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdmissionFormFieldConfiguration : IEntityTypeConfiguration<AdmissionFormField>
{
    public void Configure(EntityTypeBuilder<AdmissionFormField> builder)
    {
        builder.ToTable("AdmissionFormFields");
        builder.HasKey(field => field.Id);
        builder.Property(field => field.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(field => field.AdmissionFormId).HasColumnName("admission_form_id").IsRequired();
        builder.Property(field => field.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(field => field.Label).HasColumnName("label").HasMaxLength(150).IsRequired();
        builder.Property(field => field.Type).HasColumnName("type").HasConversion<int>();
        builder.Property(field => field.IsRequired).HasColumnName("is_required");
        builder.Property(field => field.SortOrder).HasColumnName("sort_order");

        builder.HasIndex(field => new { field.AdmissionFormId, field.Key }).IsUnique();
        builder.HasOne(field => field.AdmissionForm)
            .WithMany(form => form.Fields)
            .HasForeignKey(field => field.AdmissionFormId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
