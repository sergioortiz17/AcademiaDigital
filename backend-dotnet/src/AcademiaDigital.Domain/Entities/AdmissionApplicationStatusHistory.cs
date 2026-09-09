namespace AcademiaDigital.Domain.Entities;

public class AdmissionApplicationStatusHistory
{
    public long Id { get; set; }
    public long AdmissionApplicationId { get; set; }
    public AdmissionApplicationStatus? FromStatus { get; set; }
    public AdmissionApplicationStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public long? ChangedByUserId { get; set; }
    public string? Reason { get; set; }

    public AdmissionApplication AdmissionApplication { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
