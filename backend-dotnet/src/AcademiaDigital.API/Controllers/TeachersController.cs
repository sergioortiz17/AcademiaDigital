using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/teachers")]
public sealed class TeachersController(
    GetTeachersQueryHandler listHandler,
    GetTeacherByIdQueryHandler getHandler,
    CreateTeacherCommandHandler createHandler,
    UpdateTeacherCommandHandler updateHandler,
    DeactivateTeacherCommandHandler deactivateHandler,
    GetTeacherDocumentsQueryHandler getDocumentsHandler,
    SubmitTeacherDocumentCommandHandler submitDocumentHandler,
    ReviewTeacherDocumentCommandHandler reviewDocumentHandler,
    GetTeacherAssignmentsQueryHandler getAssignmentsHandler,
    GetMyTeacherAssignmentsQueryHandler getMyAssignmentsHandler,
    AssignTeacherCommandHandler assignHandler,
    EndTeacherAssignmentCommandHandler endAssignmentHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await listHandler.Handle(new GetTeachersQuery(includeInactive), ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await getHandler.Handle(new GetTeacherByIdQuery(id), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeacherRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await createHandler.Handle(request.ToCommand(), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTeacherRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await updateHandler.Handle(request.ToCommand(id), ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Deactivate(
        long id,
        [FromQuery, StringLength(500, MinimumLength = 3)] string? reason,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        await deactivateHandler.Handle(new DeactivateTeacherCommand(id, CurrentUserId!.Value, reason), ct);
        return NoContent();
    }

    [HttpGet("{id:long}/documents")]
    public async Task<IActionResult> GetDocuments(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await getDocumentsHandler.Handle(new GetTeacherDocumentsQuery(id), ct));
    }

    [HttpPost("{id:long}/documents")]
    public async Task<IActionResult> SubmitDocument(
        long id,
        [FromBody] SubmitTeacherDocumentRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await submitDocumentHandler.Handle(new SubmitTeacherDocumentCommand(
            id,
            request.DocumentType,
            request.FileUrl,
            request.OriginalFileName,
            request.ContentType,
            request.FileSizeBytes,
            request.ValidUntil), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPatch("{id:long}/documents/{documentId:long}/review")]
    public async Task<IActionResult> ReviewDocument(
        long id,
        long documentId,
        [FromBody] ReviewTeacherDocumentRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reviewDocumentHandler.Handle(new ReviewTeacherDocumentCommand(
            id,
            documentId,
            request.Status,
            request.Observation,
            CurrentUserId!.Value), ct));
    }

    [HttpGet("{id:long}/assignments")]
    public async Task<IActionResult> GetAssignments(
        long id,
        [FromQuery] bool includeEnded = false,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await getAssignmentsHandler.Handle(new GetTeacherAssignmentsQuery(id, includeEnded), ct));
    }

    [HttpPost("{id:long}/assignments")]
    public async Task<IActionResult> Assign(
        long id,
        [FromBody] AssignTeacherRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await assignHandler.Handle(new AssignTeacherCommand(
            id,
            request.TeachingPositionId,
            request.StartedOn,
            request.Reason,
            CurrentUserId!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("{id:long}/assignments/{assignmentId:long}")]
    public async Task<IActionResult> EndAssignment(
        long id,
        long assignmentId,
        [FromBody] EndTeacherAssignmentRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await endAssignmentHandler.Handle(new EndTeacherAssignmentCommand(
            id,
            assignmentId,
            request.EndedOn,
            request.Reason,
            CurrentUserId!.Value), ct));
    }

    [HttpGet("me/assignments")]
    public async Task<IActionResult> GetMyAssignments(
        [FromQuery] bool includeEnded = false,
        CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Profesor)
            return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await getMyAssignmentsHandler.Handle(
            new GetMyTeacherAssignmentsQuery(CurrentUserId.Value, includeEnded), ct));
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record CreateTeacherRequest(
    [Range(1, long.MaxValue)] long UserId,
    [Required, StringLength(50)] string EmployeeNumber,
    [StringLength(200)] string? Department,
    [StringLength(200)] string? SpecializationArea,
    DateTime HireDate,
    [StringLength(30)] string? PhoneNumber,
    [StringLength(255)] string? AddressLine,
    [StringLength(120)] string? City,
    [StringLength(120)] string? Province,
    [StringLength(20)] string? PostalCode,
    [StringLength(200)] string? EmergencyContactName,
    [StringLength(100)] string? EmergencyContactRelationship,
    [StringLength(30)] string? EmergencyContactPhone)
{
    public CreateTeacherCommand ToCommand() => new(
        UserId, EmployeeNumber, Department, SpecializationArea, HireDate, PhoneNumber,
        AddressLine, City, Province, PostalCode, EmergencyContactName,
        EmergencyContactRelationship, EmergencyContactPhone);
}

public sealed record UpdateTeacherRequest(
    [Required, StringLength(50)] string EmployeeNumber,
    [StringLength(200)] string? Department,
    [StringLength(200)] string? SpecializationArea,
    DateTime HireDate,
    [StringLength(30)] string? PhoneNumber,
    [StringLength(255)] string? AddressLine,
    [StringLength(120)] string? City,
    [StringLength(120)] string? Province,
    [StringLength(20)] string? PostalCode,
    [StringLength(200)] string? EmergencyContactName,
    [StringLength(100)] string? EmergencyContactRelationship,
    [StringLength(30)] string? EmergencyContactPhone)
{
    public UpdateTeacherCommand ToCommand(long teacherId) => new(
        teacherId, EmployeeNumber, Department, SpecializationArea, HireDate, PhoneNumber,
        AddressLine, City, Province, PostalCode, EmergencyContactName,
        EmergencyContactRelationship, EmergencyContactPhone);
}

public sealed record SubmitTeacherDocumentRequest(
    [Required, StringLength(50, MinimumLength = 1)] string DocumentType,
    [Required, StringLength(1000, MinimumLength = 1)] string FileUrl,
    [Required, StringLength(255, MinimumLength = 1)] string OriginalFileName,
    [Required, StringLength(100, MinimumLength = 1)] string ContentType,
    [Range(1, 10 * 1024 * 1024)] long FileSizeBytes,
    DateOnly? ValidUntil);

public sealed record ReviewTeacherDocumentRequest(
    StudentDocumentStatus Status,
    [StringLength(1000)] string? Observation);

public sealed record AssignTeacherRequest(
    [Range(1, int.MaxValue)] int TeachingPositionId,
    DateOnly StartedOn,
    [StringLength(500)] string? Reason);

public sealed record EndTeacherAssignmentRequest(
    DateOnly EndedOn,
    [Required, StringLength(500, MinimumLength = 3)] string Reason);
