namespace AcademiaDigital.Domain.Entities;

public sealed class AdmissionAgreement
{
    public long Id { get; set; }
    public long AdmissionApplicationId { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = "{}";
    public AdmissionAgreementStatus Status { get; set; } = AdmissionAgreementStatus.Pending;
    public string? StorageKey { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string? Sha256 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string? LastError { get; set; }

    public AdmissionApplication AdmissionApplication { get; set; } = null!;

    public void MarkReady(string storageKey, string sha256, DateTime generatedAt)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("Storage key and SHA-256 are required for a generated agreement.");
        StorageKey = storageKey;
        Sha256 = sha256;
        GeneratedAt = generatedAt;
        Status = AdmissionAgreementStatus.Ready;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Status = AdmissionAgreementStatus.Failed;
        LastError = string.IsNullOrWhiteSpace(error) ? "Agreement generation failed." : error.Trim();
    }
}

public enum AdmissionAgreementStatus
{
    Pending = 0,
    Ready = 1,
    Failed = 2
}
