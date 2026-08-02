using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AcademiaDigital.API.Controllers.Requests;

/// <summary>
/// Multipart/form-data request for the career CSV import. Lives in the API project (not
/// Application/Dtos) because it holds an IFormFile, an ASP.NET Core web type that the
/// Application layer intentionally does not depend on.
/// </summary>
public sealed class ImportCareerCsvRequest
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

    [Required]
    [MaxLength(20)]
    public string StudyPlanCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string StudyPlanName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int VersionNumber { get; set; } = 1;

    public DateOnly? EffectiveFrom { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
