using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class ActiveSessionConfiguration : IEntityTypeConfiguration<ActiveSession>
{
    public void Configure(EntityTypeBuilder<ActiveSession> builder)
    {
        // Mapea a la misma tabla del Django para compatibilidad con la DB existente
        builder.ToTable("ActiveSessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.Token).HasColumnName("token").HasMaxLength(255).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("date");

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
