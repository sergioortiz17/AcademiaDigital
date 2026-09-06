using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AcademiaDigital.API.Controllers.Requests;

/// <summary>
/// Multipart/form-data request for the diff-preview endpoint: same CSV shape as an import, but
/// nothing gets persisted — it's compared in-memory against an existing study plan.
/// </summary>
public sealed class DiffPreviewCsvRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
