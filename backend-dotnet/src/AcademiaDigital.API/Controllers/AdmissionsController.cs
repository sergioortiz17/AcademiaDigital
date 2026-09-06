using System.ComponentModel.DataAnnotations;
using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Admissions;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/admissions")]
public sealed class AdmissionsController(
    GetAdmissionFormQueryHandler getFormHandler,
    CreateAdmissionApplicationCommandHandler createApplicationHandler,
    GetAdmissionFormsQueryHandler getFormsHandler,
    CreateAdmissionFormCommandHandler createFormHandler,
    SetAdmissionFormActiveCommandHandler setFormActiveHandler,
    SetAdmissionFormCapacityCommandHandler setFormCapacityHandler,
    GetAdmissionApplicationsQueryHandler getApplicationsHandler,
    GetAdmissionApplicationQueryHandler getApplicationHandler,
    ChangeAdmissionApplicationStatusCommandHandler changeApplicationStatusHandler,
    ProcessAdmissionExpirationsCommandHandler processExpirationsHandler,
    GetAdmissionApplicationDocumentsQueryHandler getApplicationDocumentsHandler,
    SubmitAdmissionApplicationDocumentCommandHandler submitApplicationDocumentHandler,
    ReviewAdmissionApplicationDocumentCommandHandler reviewApplicationDocumentHandler,
    GetAdmissionAgreementQueryHandler getAgreementHandler,
    DownloadAdmissionAgreementQueryHandler downloadAgreementHandler,
    ProcessAdmissionOutboxCommandHandler processOutboxHandler) : ApiControllerBase
{
    [HttpGet("forms/{slug}")]
    public async Task<IActionResult> GetForm(string slug, CancellationToken ct)
    {
        var result = await getFormHandler.Handle(new GetAdmissionFormQuery(slug), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("applications")]
    [EnableRateLimiting("PublicAdmissionSubmission")]
    public async Task<IActionResult> CreateApplication(
        [FromBody] CreateAdmissionApplicationRequest request,
        CancellationToken ct)
    {
        var result = await createApplicationHandler.Handle(
            new CreateAdmissionApplicationCommand(
                request.FormSlug,
                request.AcceptedTerms,
                request.Fields,
                request.ChallengeToken,
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    [HttpGet("forms")]
    public async Task<IActionResult> GetForms(CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await getFormsHandler.Handle(new GetAdmissionFormsQuery(), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("forms")]
    public async Task<IActionResult> CreateForm(
        [FromBody] CreateAdmissionFormRequest request,
        CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await createFormHandler.Handle(
            new CreateAdmissionFormCommand(
                request.CareerId,
                request.CommissionId,
                request.Slug,
                request.Title,
                request.Description,
                request.TermsText,
                request.ReservationHours,
                request.Capacity,
                request.Fields.Select(field => new AdmissionFormFieldInput(
                    field.Key,
                    field.Label,
                    field.Type,
                    field.IsRequired,
                    field.SortOrder)).ToArray()),
            ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    [HttpPatch("forms/{formId:int}/capacity")]
    public async Task<IActionResult> SetFormCapacity(
        int formId,
        [FromBody] SetAdmissionFormCapacityRequest request,
        CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await setFormCapacityHandler.Handle(
            new SetAdmissionFormCapacityCommand(formId, request.Capacity, CurrentUserId!.Value), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPatch("forms/{formId:int}/active")]
    public async Task<IActionResult> SetFormActive(
        int formId,
        [FromBody] SetAdmissionFormActiveRequest request,
        CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await setFormActiveHandler.Handle(
            new SetAdmissionFormActiveCommand(formId, request.IsActive), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] int? admissionFormId,
        [FromQuery] AdmissionApplicationStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await getApplicationsHandler.Handle(
            new GetAdmissionApplicationsQuery(admissionFormId, status, search, page, pageSize), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("applications/{publicId:guid}")]
    public async Task<IActionResult> GetApplication(Guid publicId, CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await getApplicationHandler.Handle(new GetAdmissionApplicationQuery(publicId), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPatch("applications/{publicId:guid}/status")]
    public async Task<IActionResult> ChangeApplicationStatus(
        Guid publicId,
        [FromBody] ChangeAdmissionApplicationStatusRequest request,
        CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await changeApplicationStatusHandler.Handle(
            new ChangeAdmissionApplicationStatusCommand(
                publicId,
                request.Status,
                request.Reason,
                CurrentUserId!.Value),
            ct);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("applications/{publicId:guid}/documents")]
    public async Task<IActionResult> GetApplicationDocuments(Guid publicId, CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await getApplicationDocumentsHandler.Handle(
            new GetAdmissionApplicationDocumentsQuery(publicId), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("applications/{publicId:guid}/documents")]
    public async Task<IActionResult> SubmitApplicationDocument(
        Guid publicId,
        [FromBody] SubmitAdmissionApplicationDocumentRequest request,
        CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await submitApplicationDocumentHandler.Handle(
            new SubmitAdmissionApplicationDocumentCommand(
                publicId,
                request.DocumentRequirementId,
                request.FileUrl,
                request.OriginalFileName,
                request.ContentType,
                request.FileSizeBytes),
            ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    [HttpPatch("applications/{publicId:guid}/documents/{documentId:long}/review")]
    public async Task<IActionResult> ReviewApplicationDocument(
        Guid publicId,
        long documentId,
        [FromBody] ReviewAdmissionApplicationDocumentRequest request,
        CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await reviewApplicationDocumentHandler.Handle(
            new ReviewAdmissionApplicationDocumentCommand(
                publicId,
                documentId,
                request.Status,
                request.Observation,
                CurrentUserId!.Value),
            ct);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("applications/{publicId:guid}/agreement")]
    public async Task<IActionResult> GetAgreement(Guid publicId, CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await getAgreementHandler.Handle(new GetAdmissionAgreementQuery(publicId), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("applications/{publicId:guid}/agreement/download")]
    public async Task<IActionResult> DownloadAgreement(Guid publicId, CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await downloadAgreementHandler.Handle(new DownloadAdmissionAgreementQuery(publicId), ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("outbox/process")]
    public async Task<IActionResult> ProcessOutbox([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await processOutboxHandler.Handle(new ProcessAdmissionOutboxCommand(limit), ct);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("applications/process-expirations")]
    public async Task<IActionResult> ProcessExpirations(
        [FromQuery] int? admissionFormId,
        CancellationToken ct)
    {
        var denial = RequireAdmin();
        if (denial is not null) return denial;

        var result = await processExpirationsHandler.Handle(
            new ProcessAdmissionExpirationsCommand(admissionFormId, CurrentUserId!.Value), ct);
        return Ok(new { success = true, data = result });
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Admin)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
        return null;
    }
}

public sealed record CreateAdmissionApplicationRequest(
    [Required][StringLength(100, MinimumLength = 3)] string FormSlug,
    bool AcceptedTerms,
    [Required] IReadOnlyDictionary<string, string?> Fields,
    [StringLength(4096)] string? ChallengeToken = null);

public sealed record AdmissionFormFieldRequest(
    [Required][StringLength(100, MinimumLength = 1)] string Key,
    [Required][StringLength(150, MinimumLength = 1)] string Label,
    AdmissionFieldType Type,
    bool IsRequired,
    [Range(0, 1000)] int SortOrder);

public sealed record CreateAdmissionFormRequest(
    [Range(1, int.MaxValue)] int CareerId,
    [Range(1, int.MaxValue)] int? CommissionId,
    [Required][StringLength(100, MinimumLength = 3)] string Slug,
    [Required][StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(1000)] string? Description,
    [Required][StringLength(8000, MinimumLength = 1)] string TermsText,
    [Range(1, 720)] int ReservationHours,
    [Range(1, 100000)] int? Capacity,
    [Required][MinLength(2)] IReadOnlyList<AdmissionFormFieldRequest> Fields);

public sealed record SetAdmissionFormActiveRequest(bool IsActive);

public sealed record SetAdmissionFormCapacityRequest([Range(1, 100000)] int? Capacity);

public sealed record ChangeAdmissionApplicationStatusRequest(
    AdmissionApplicationStatus Status,
    [StringLength(500)] string? Reason);

public sealed record SubmitAdmissionApplicationDocumentRequest(
    [Range(1, int.MaxValue)] int DocumentRequirementId,
    [Required][StringLength(1000, MinimumLength = 1)] string FileUrl,
    [Required][StringLength(255, MinimumLength = 1)] string OriginalFileName,
    [Required][StringLength(100, MinimumLength = 1)] string ContentType,
    [Range(1, long.MaxValue)] long FileSizeBytes);

public sealed record ReviewAdmissionApplicationDocumentRequest(
    StudentDocumentStatus Status,
    [StringLength(1000)] string? Observation);
