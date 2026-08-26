namespace AcademiaDigital.Domain.Entities;

public class CertificateRequest
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string CertificateType { get; set; } = string.Empty;
    public CertificateKind Kind { get; set; }
    public long? StudentCareerId { get; set; }
    public long? ExamRegistrationId { get; set; }
    public CertificateStatus Status { get; set; } = CertificateStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    public User User { get; set; } = null!;
    public StudentCareer? StudentCareer { get; set; }
    public ExamRegistration? ExamRegistration { get; set; }
    public User? ReviewedByUser { get; set; }
    public CertificateIssuance? Issuance { get; set; }

    public void Approve(long actorUserId, DateTime nowUtc)
    {
        if (Status != CertificateStatus.Pending)
            throw new InvalidOperationException("Only pending certificate requests can be approved.");
        Status = CertificateStatus.Approved;
        ReviewedAt = UpdatedAt = nowUtc;
        ReviewedByUserId = actorUserId;
        RejectionReason = null;
    }

    public void Reject(long actorUserId, string reason, DateTime nowUtc)
    {
        if (Status != CertificateStatus.Pending)
            throw new InvalidOperationException("Only pending certificate requests can be rejected.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required.");
        Status = CertificateStatus.Rejected;
        ReviewedAt = UpdatedAt = nowUtc;
        ReviewedByUserId = actorUserId;
        RejectionReason = reason.Trim();
    }

    public void MarkIssuing(DateTime nowUtc)
    {
        if (Status != CertificateStatus.Approved)
            throw new InvalidOperationException("Only approved certificate requests can be issued.");
        Status = CertificateStatus.Issuing;
        UpdatedAt = nowUtc;
    }

    public void MarkIssued(DateTime nowUtc)
    {
        if (Status != CertificateStatus.Issuing)
            throw new InvalidOperationException("Only a certificate being generated can be marked as issued.");
        Status = CertificateStatus.Issued;
        UpdatedAt = nowUtc;
    }
}

public enum CertificateStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Issuing = 3,
    Issued = 4
}

public enum CertificateKind
{
    RegularStudent = 0,
    Enrollment = 1,
    ApprovedCourses = 2,
    AcademicStatus = 3,
    Transcript = 4,
    GeneralAcademicStatus = 5,
    ExamPermit = 6
}

public sealed class CertificateIssuance
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CertificateRequestId { get; set; }
    public CertificateRequest CertificateRequest { get; set; } = null!;
    public long SequenceNumber { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public CertificateIssuanceStatus Status { get; set; } = CertificateIssuanceStatus.Generating;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string? StorageKey { get; set; }
    public string? Sha256 { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public long IssuedByUserId { get; set; }
    public User IssuedByUser { get; set; } = null!;

    public void MarkReady(string storageKey, string sha256, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("Certificate storage key and SHA-256 are required.");
        StorageKey = storageKey;
        Sha256 = sha256;
        Status = CertificateIssuanceStatus.Ready;
        GeneratedAt = nowUtc;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Status = CertificateIssuanceStatus.Failed;
        LastError = string.IsNullOrWhiteSpace(error) ? "Certificate generation failed." : error[..Math.Min(error.Length, 1000)];
    }
}

public enum CertificateIssuanceStatus
{
    Generating = 0,
    Ready = 1,
    Failed = 2
}

public sealed class CertificateSequence
{
    public int Id { get; set; } = 1;
    public long LastValue { get; set; }

    public long TakeNext()
    {
        checked { LastValue++; }
        return LastValue;
    }
}
