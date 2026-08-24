using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(message => message.Type).HasColumnName("type").HasMaxLength(200).IsRequired();
        builder.Property(message => message.AggregateId).HasColumnName("aggregate_id").HasMaxLength(100).IsRequired();
        builder.Property(message => message.DeduplicationKey).HasColumnName("deduplication_key").HasMaxLength(200).IsRequired();
        builder.Property(message => message.PayloadJson).HasColumnName("payload_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(message => message.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(message => message.OccurredAt).HasColumnName("occurred_at");
        builder.Property(message => message.AvailableAt).HasColumnName("available_at");
        builder.Property(message => message.ProcessingStartedAt).HasColumnName("processing_started_at");
        builder.Property(message => message.ProcessedAt).HasColumnName("processed_at");
        builder.Property(message => message.Attempts).HasColumnName("attempts");
        builder.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(2000);
        builder.Property(message => message.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(message => message.DeduplicationKey).IsUnique();
        builder.HasIndex(message => new { message.Status, message.AvailableAt });
    }
}
