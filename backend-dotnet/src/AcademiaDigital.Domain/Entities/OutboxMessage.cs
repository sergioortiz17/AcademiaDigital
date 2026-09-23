namespace AcademiaDigital.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string DeduplicationKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public DateTime OccurredAt { get; set; }
    public DateTime AvailableAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public void MarkProcessing(DateTime now)
    {
        Status = OutboxMessageStatus.Processing;
        ProcessingStartedAt = now;
        Attempts++;
        LastError = null;
    }

    public void MarkProcessed(DateTime now)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = now;
        LastError = null;
    }

    public void MarkFailed(string error, DateTime retryAt)
    {
        Status = OutboxMessageStatus.Failed;
        AvailableAt = retryAt;
        LastError = string.IsNullOrWhiteSpace(error) ? "Outbox delivery failed." : error.Trim();
    }
}

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3
}
