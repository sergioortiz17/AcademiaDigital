using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> builder)
    {
        builder.ToTable("Communications");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(c => c.Content).HasColumnName("content").IsRequired();
        builder.Property(c => c.Type).HasColumnName("type").HasConversion<int>();
        builder.Property(c => c.PublishedAt).HasColumnName("published_at");
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at");
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.AuthorId).HasColumnName("author_id");

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
