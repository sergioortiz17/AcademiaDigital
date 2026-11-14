namespace AcademiaDigital.Finance.Domain.Entities;

public sealed class AcademicStudentSnapshot
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public long StudentCareerId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string LegajoNumber { get; set; } = string.Empty;
    public int CareerId { get; set; }
    public string CareerName { get; set; } = string.Empty;
    public int? CommissionId { get; set; }
    public string? CommissionName { get; set; }
    public int AcademicYear { get; set; }
    public bool IsActive { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}

public sealed class DelinquencyAlertRule
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MinimumOutstandingAmount { get; set; }
    public int MinimumDaysOverdue { get; set; }
    public bool IsActive { get; set; } = true;
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
