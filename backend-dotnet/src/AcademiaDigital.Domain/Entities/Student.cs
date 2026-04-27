namespace AcademiaDigital.Domain.Entities;

public class Student
{
    public long Id { get; set; }
    public string LegajoNumber { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
}

public enum StudentStatus
{
    Active = 0,
    Inactive = 1,
    Graduated = 2,
    Suspended = 3
}
