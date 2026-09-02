namespace AcademiaDigital.Domain.Entities;

public class AdmissionForm
{
    public int Id { get; set; }
    public int CareerId { get; set; }
    public int? CommissionId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TermsText { get; set; } = string.Empty;
    public int ReservationHours { get; set; } = 72;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public Career Career { get; set; } = null!;
    public Commission? Commission { get; set; }
    public ICollection<AdmissionFormField> Fields { get; set; } = [];
    public ICollection<AdmissionApplication> Applications { get; set; } = [];
}

public class AdmissionFormField
{
    public int Id { get; set; }
    public int AdmissionFormId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public AdmissionFieldType Type { get; set; } = AdmissionFieldType.Text;
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    public AdmissionForm AdmissionForm { get; set; } = null!;
}

public enum AdmissionFieldType
{
    Text = 0,
    Email = 1,
    Phone = 2,
    Date = 3,
    Checkbox = 4
}
