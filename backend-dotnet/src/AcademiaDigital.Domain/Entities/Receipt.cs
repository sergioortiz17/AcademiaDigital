using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceiptStatus
{
    Generating = 0,
    Ready = 1,
    Failed = 2
}

public sealed class Receipt
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public long SequenceNumber { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Generating;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string? StorageKey { get; set; }
    public string? Sha256 { get; set; }
    public string? LastError { get; set; }
    public string? FiscalCae { get; set; }
    public string? FiscalQrData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public long IssuedByUserId { get; set; }
    public User IssuedByUser { get; set; } = null!;

    public void MarkReady(string storageKey, string sha256, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Receipt storage key is required.");
        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
            throw new ArgumentException("Receipt SHA-256 must contain 64 hexadecimal characters.");
        StorageKey = storageKey;
        Sha256 = sha256.ToUpperInvariant();
        Status = ReceiptStatus.Ready;
        GeneratedAt = nowUtc;
        LastError = null;
    }

    public void MarkFailed(string? error)
    {
        Status = ReceiptStatus.Failed;
        StorageKey = null;
        Sha256 = null;
        GeneratedAt = null;
        var message = string.IsNullOrWhiteSpace(error) ? "Receipt generation failed." : error.Trim();
        LastError = message[..Math.Min(message.Length, 1000)];
    }
}

public sealed class ReceiptSequence
{
    public int Id { get; set; } = 1;
    public long LastValue { get; set; }

    public long TakeNext()
    {
        checked { LastValue++; }
        return LastValue;
    }
}
