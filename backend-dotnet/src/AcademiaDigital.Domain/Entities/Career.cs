namespace AcademiaDigital.Domain.Entities;

public class Career
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalCredits { get; set; }
    public int DurationYears { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Subject> Subjects { get; set; } = [];
    public ICollection<Student> Students { get; set; } = [];
}
