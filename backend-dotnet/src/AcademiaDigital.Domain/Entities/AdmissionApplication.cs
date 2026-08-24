namespace AcademiaDigital.Domain.Entities;

public class AdmissionApplication
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int AdmissionFormId { get; set; }
    public string ApplicantEmail { get; set; } = string.Empty;
    public string ApplicantDni { get; set; } = string.Empty;
    public string SubmittedFieldsJson { get; set; } = "{}";
    public AdmissionApplicationStatus Status { get; set; } = AdmissionApplicationStatus.PreEnrolled;
    public DateTime TermsAcceptedAt { get; set; }
    public DateTime? ReservationExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AdmissionForm AdmissionForm { get; set; } = null!;
    public ICollection<AdmissionApplicationStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<AdmissionApplicationDocument> Documents { get; set; } = [];
    public AdmissionAgreement? Agreement { get; set; }
}

public enum AdmissionApplicationStatus
{
    PreEnrolled = 0,
    Enrolled = 1,
    Confirmed = 2,
    Waitlisted = 3,
    Expired = 4,
    Rejected = 5
}
