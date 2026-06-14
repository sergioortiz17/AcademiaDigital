namespace AcademiaDigital.Application.Dtos;

public class CareerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalCredits { get; set; }
    public int DurationYears { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
