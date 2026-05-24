using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Certificates;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/certificates")]
public class CertificatesController(
    GetCertificateRequestsUseCase getRequestsUseCase,
    CreateCertificateRequestUseCase createRequestUseCase,
    GetAllCertificateRequestsUseCase getAllRequestsUseCase) : ApiControllerBase
{
    // GET /api/v1/certificates/my  — alumno ve sus solicitudes
    [HttpGet("my")]
    public async Task<IActionResult> GetMyCertificates(CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        var requests = await getRequestsUseCase.ExecuteAsync(CurrentUserId.Value, ct);
        return Ok(new { success = true, requests });
    }

    // POST /api/v1/certificates/request  — alumno solicita certificado
    [HttpPost("request")]
    public async Task<IActionResult> RequestCertificate([FromBody] CreateCertificateRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Alumno) return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Only students can request certificates."));
        var result = await createRequestUseCase.ExecuteAsync(CurrentUserId.Value, request.CertificateType, ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, request = result });
    }

    // GET /api/v1/certificates/all?search=&status=  — admin ve todas las solicitudes
    [HttpGet("all")]
    public async Task<IActionResult> GetAllCertificates(
        [FromQuery] string? search,
        [FromQuery] CertificateStatus? status,
        CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Admin) return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
        var requests = await getAllRequestsUseCase.ExecuteAsync(search, status, ct);
        return Ok(new { success = true, requests });
    }
}

public record CreateCertificateRequest([Required][MaxLength(100)] string CertificateType);
