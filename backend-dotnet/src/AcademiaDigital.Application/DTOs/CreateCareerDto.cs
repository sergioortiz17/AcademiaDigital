using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.Application.Dtos;

public class CreateCareerDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalCredits { get; set; }

    [Range(1, 20)]
    public int DurationYears { get; set; }
}
