namespace AcademiaDigital.Domain.Entities;

public class Teacher
{
    public long Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? SpecializationArea { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PhoneNumber { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public long? DeactivatedByUserId { get; set; }
    public string? DeactivationReason { get; set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;
}
