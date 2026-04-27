namespace AcademiaDigital.Domain.Entities;

public class Administrative
{
    public long Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    public long UserId { get; set; }
    public User User { get; set; } = null!;
}
