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
    // Domicilio, contacto de emergencia y obra social están en User (los carga cada usuario).
    public DateTime? DeactivatedAt { get; set; }
    public long? DeactivatedByUserId { get; set; }
    public string? DeactivationReason { get; set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;
}
