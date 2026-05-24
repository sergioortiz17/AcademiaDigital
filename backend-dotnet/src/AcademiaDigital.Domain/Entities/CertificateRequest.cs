namespace AcademiaDigital.Domain.Entities;

public class CertificateRequest
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string CertificateType { get; set; } = string.Empty;
    public CertificateStatus Status { get; set; } = CertificateStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}

public enum CertificateStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
