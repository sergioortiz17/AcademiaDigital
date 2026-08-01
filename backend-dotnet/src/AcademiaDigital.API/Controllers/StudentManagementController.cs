using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/students/{studentId:long}")]
public class StudentManagementController(IStudentManagementService service) : ApiControllerBase
{
    [HttpPatch("status")]
    public async Task<IActionResult> ChangeStatus(long studentId, ChangeStudentStatusRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.ChangeStatusAsync(studentId, request.Status, request.Reason, CurrentUserId!.Value, ct)); }

    [HttpGet("status-history")]
    public async Task<IActionResult> StatusHistory(long studentId, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetStatusHistoryAsync(studentId, ct)); }

    [HttpGet("record")]
    public async Task<IActionResult> Record(long studentId, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetRecordAsync(studentId, ct)); }

    [HttpPost("academic-assignments")]
    public async Task<IActionResult> Assign(long studentId, CreateAcademicAssignmentRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.AssignAcademicAsync(studentId, request, CurrentUserId!.Value, ct); return Created("", x); }

    [HttpGet("academic-assignments")]
    public async Task<IActionResult> Assignments(long studentId, [FromQuery] int? academicYear, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetAssignmentsAsync(studentId, academicYear, ct)); }

    [HttpGet("documents")]
    public async Task<IActionResult> Documents(long studentId, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetDocumentsAsync(studentId, ct)); }

    [HttpPost("documents")]
    public async Task<IActionResult> AddDocument(long studentId, CreateStudentDocumentRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.AddDocumentAsync(studentId, request, ct); return Created("", x); }

    [HttpGet("documents/{documentId:long}")]
    public async Task<IActionResult> Document(long studentId, long documentId, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; var x = (await service.GetDocumentsAsync(studentId, ct)).SingleOrDefault(d => d.Id == documentId); return x is null ? NotFound() : Ok(x); }

    [HttpPatch("documents/{documentId:long}/status")]
    public async Task<IActionResult> Review(long studentId, long documentId, ReviewStudentDocumentRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.ReviewDocumentAsync(studentId, documentId, request, CurrentUserId!.Value, ct)); }

    [HttpDelete("documents/{documentId:long}")]
    public async Task<IActionResult> DeleteDocument(long studentId, long documentId, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; await service.DeleteDocumentAsync(studentId, documentId, ct); return NoContent(); }

    [HttpGet("pending-documents")]
    public async Task<IActionResult> Pending(long studentId, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetPendingDocumentsAsync(studentId, ct)); }

    [HttpGet("scholarships")]
    public async Task<IActionResult> Scholarships(long studentId, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetStudentScholarshipsAsync(studentId, ct)); }

    [HttpPost("scholarships")]
    public async Task<IActionResult> AddScholarship(long studentId, UpsertStudentScholarshipRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.SaveStudentScholarshipAsync(studentId, null, request, CurrentUserId!.Value, ct); return Created("", x); }

    [HttpPut("scholarships/{id:long}")]
    public async Task<IActionResult> UpdateScholarship(long studentId, long id, UpsertStudentScholarshipRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.SaveStudentScholarshipAsync(studentId, id, request, CurrentUserId!.Value, ct)); }

    [HttpDelete("scholarships/{id:long}")]
    public async Task<IActionResult> RevokeScholarship(long studentId, long id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; await service.RevokeStudentScholarshipAsync(studentId, id, CurrentUserId!.Value, ct); return NoContent(); }

    [HttpGet("custom-values")]
    public async Task<IActionResult> CustomValues(long studentId, CancellationToken ct)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetCustomValuesAsync(studentId, ct)); }

    [HttpPut("custom-values")]
    public async Task<IActionResult> SaveCustomValues(long studentId, UpsertCustomValuesRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.SaveCustomValuesAsync(studentId, request, CurrentUserId!.Value, ct)); }

    [HttpGet("academic-history")]
    public async Task<IActionResult> History(long studentId, [FromQuery] int? academicYear, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    { var g = await Reader(studentId, ct); if (g is not null) return g; return Ok(await service.GetAcademicHistoryAsync(studentId, academicYear, page, pageSize, ct)); }

    private IActionResult? Admin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
    private async Task<IActionResult?> Reader(long studentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole == UserRole.Admin || await service.IsOwnerAsync(studentId, CurrentUserId.Value, ct)) return null;
        return StatusCode(StatusCodes.Status403Forbidden);
    }
}
