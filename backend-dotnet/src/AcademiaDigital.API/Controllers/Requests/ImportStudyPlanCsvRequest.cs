using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AcademiaDigital.API.Controllers.Requests;

/// <summary>
/// Multipart/form-data request for importing a study plan (courses CSV) into an already existing
/// career. Lives in the API project (not Application/Dtos) because it holds an IFormFile, an
/// ASP.NET Core web type the Application layer intentionally does not depend on.
///
/// Code is a free-form string (only bound by the 20-char column limit on StudyPlans.code) so an
/// admin can enter a real-world reference such as a Resolución Ministerial number
/// (e.g. "RM 999/13") instead of being forced into a synthetic "XXXX-V1" pattern.
/// </summary>
public sealed class ImportStudyPlanCsvRequest
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int VersionNumber { get; set; } = 1;

    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
