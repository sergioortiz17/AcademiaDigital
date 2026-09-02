using System.ComponentModel.DataAnnotations;
using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Certificates;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/certificates")]
public sealed class CertificatesController(
    GetCertificateRequestsUseCase getRequestsUseCase,
    CreateCertificateRequestUseCase createRequestUseCase,
    GetAllCertificateRequestsUseCase getAllRequestsUseCase,
    ReviewCertificateRequestCommandHandler reviewHandler,
    IssueCertificateCommandHandler issueHandler,
    GetCertificateHistoryQueryHandler historyHandler,
    DownloadCertificateQueryHandler downloadHandler) : ApiControllerBase
{
    [HttpGet("my")]
    public async Task<IActionResult> GetMyCertificates(CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        var requests = await getRequestsUseCase.ExecuteAsync(CurrentUserId.Value, ct);
        return Ok(new { success = true, requests });
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestCertificate([FromBody] CreateCertificateRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Alumno)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Only students can request certificates."));
        var result = await createRequestUseCase.ExecuteAsync(
            CurrentUserId.Value, request.CertificateType, request.StudentCareerId, request.ExamRegistrationId, ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, request = result });
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllCertificates(
        [FromQuery] string? search,
        [FromQuery] CertificateRequestStatusFilter? status,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var requests = await getAllRequestsUseCase.ExecuteAsync(
            search,
            status is null ? null : (CertificateStatus)status.Value,
            ct);
        return Ok(new { success = true, requests });
    }

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reviewHandler.Handle(new ReviewCertificateRequestCommand(
            id, true, null, CurrentUserId!.Value), ct));
    }

    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectCertificateRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reviewHandler.Handle(new ReviewCertificateRequestCommand(
            id, false, request.Reason, CurrentUserId!.Value), ct));
    }

    [HttpPost("{id:long}/issue")]
    public async Task<IActionResult> Issue(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await issueHandler.Handle(new IssueCertificateCommand(id, CurrentUserId!.Value), ct));
    }

    [HttpGet("issued/me")]
    public async Task<IActionResult> GetMyIssuedCertificates(CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Alumno) return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await historyHandler.Handle(new GetCertificateHistoryQuery(
            CurrentUserId.Value, false), ct));
    }

    [HttpGet("students/{studentId:long}/history")]
    public async Task<IActionResult> GetStudentHistory(long studentId, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await historyHandler.Handle(new GetCertificateHistoryQuery(
            CurrentUserId!.Value, true, studentId), ct));
    }

    [HttpGet("issued/{publicId:guid}/download")]
    public async Task<IActionResult> Download(Guid publicId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole is not (UserRole.Admin or UserRole.Alumno))
            return StatusCode(StatusCodes.Status403Forbidden);
        var file = await downloadHandler.Handle(new DownloadCertificateQuery(
            publicId, CurrentUserId.Value, CurrentUserRole == UserRole.Admin), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
    }
}

public sealed record CreateCertificateRequest(
    [Required, StringLength(100, MinimumLength = 1)] string CertificateType,
    [Range(1, long.MaxValue)] long? StudentCareerId = null,
    [Range(1, long.MaxValue)] long? ExamRegistrationId = null);

public sealed record RejectCertificateRequest(
    [Required, StringLength(1000, MinimumLength = 3)] string Reason);

/// <summary>
/// Public filter kept compatible with the Angular contract. Issuing and Issued
/// remain internal workflow states and are projected as Approved.
/// </summary>
public enum CertificateRequestStatusFilter
{
    Pending,
    Approved,
    Rejected
}
