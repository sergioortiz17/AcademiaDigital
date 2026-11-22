using AcademiaDigital.Application.Services;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>
/// Requisitos de ingreso: el alumno sube sus documentos (analítico, etc.) y Administración los revisa.
/// La revisión (aprobar/rechazar) usa PATCH /api/v1/students/{id}/documents/{documentId}/status.
/// </summary>
[Route("api/v1")]
public sealed class AdmissionDocumentsController(
    StudentAdmissionDocumentsUseCase useCase,
    IStudentManagementService service) : ApiControllerBase
{
    // GET /api/v1/students/me/admission-documents — requisitos de su carrera y documentos enviados
    [HttpGet("students/me/admission-documents")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Alumno) return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await useCase.GetMineAsync(CurrentUserId.Value, ct));
    }

    // POST /api/v1/students/me/admission-documents (multipart: requirementId, file)
    [HttpPost("students/me/admission-documents")]
    [RequestSizeLimit(UploadedDocumentPolicy.MaximumFileSizeBytes + 64 * 1024)]
    public async Task<IActionResult> Upload([FromForm] int requirementId, IFormFile? file, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Alumno) return StatusCode(StatusCodes.Status403Forbidden);
        if (file is null || file.Length == 0)
            return BadRequest(new { success = false, msg = "Seleccione un archivo PDF, JPG o PNG." });
        if (file.Length > UploadedDocumentPolicy.MaximumFileSizeBytes)
            return BadRequest(new { success = false, msg = "El archivo debe pesar hasta 10 MB." });
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var created = await useCase.UploadMineAsync(CurrentUserId.Value, requirementId, file.FileName, buffer.ToArray(), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    // GET /api/v1/students/me/admission-documents/{documentId}/file
    [HttpGet("students/me/admission-documents/{documentId:long}/file")]
    public async Task<IActionResult> DownloadMine(long documentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var studentId = await useCase.GetMyStudentIdAsync(CurrentUserId.Value, ct);
        var file = await useCase.ReadAsync(studentId, documentId, CurrentUserId.Value, false, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // GET /api/v1/students/{studentId}/documents/{documentId}/file — Admin o el alumno dueño
    [HttpGet("students/{studentId:long}/documents/{documentId:long}/file")]
    public async Task<IActionResult> Download(long studentId, long documentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var file = await useCase.ReadAsync(studentId, documentId, CurrentUserId.Value, CurrentUserRole == UserRole.Admin, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // GET /api/v1/admin/student-documents?status=Submitted — bandeja de revisión
    [HttpGet("admin/student-documents")]
    public async Task<IActionResult> ForReview([FromQuery] StudentDocumentStatus? status, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Admin) return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await service.GetDocumentsForReviewAsync(status, ct));
    }
}
