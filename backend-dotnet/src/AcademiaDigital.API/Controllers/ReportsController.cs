using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>Listados para exportar a Excel (el archivo lo arma el navegador).</summary>
[Route("api/v1/admin/reports")]
public sealed class ReportsController(IStudentReportQuery students) : ApiControllerBase
{
    // GET /api/v1/admin/reports/students?kind=Students|Applicants&status=Graduated&careerId=1&from=2026-01-01&to=2026-12-31
    [HttpGet("students")]
    public async Task<IActionResult> Students(
        [FromQuery] StudentReportKind kind = StudentReportKind.Students,
        [FromQuery] StudentStatus? status = null,
        [FromQuery] int? careerId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Admin) return StatusCode(StatusCodes.Status403Forbidden);
        if (from.HasValue && to.HasValue && to < from)
            return BadRequest(new { success = false, msg = "La fecha 'hasta' no puede ser anterior a 'desde'." });
        return Ok(await students.GetAsync(new StudentReportFilter(kind, status, careerId, from, to), ct));
    }
}
