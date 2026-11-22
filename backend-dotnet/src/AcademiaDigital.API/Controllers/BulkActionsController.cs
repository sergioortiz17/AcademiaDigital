using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>Acciones masivas de Administración sobre alumnos.</summary>
[Route("api/v1/admin/bulk")]
public sealed class BulkActionsController(IBulkStudentActionsService service) : ApiControllerBase
{
    protected override Permission? Area => Permission.ManageStudents;

    // POST /api/v1/admin/bulk/graduate — { studentIds, graduatedOn, reason }
    [HttpPost("graduate")]
    public async Task<IActionResult> Graduate([FromBody] BulkGraduateRequest request, CancellationToken ct)
    {
        var guard = Admin(); if (guard is not null) return guard;
        return Ok(await service.GraduateAsync(request.StudentIds, request.GraduatedOn, request.Reason, CurrentUserId!.Value, ct));
    }

    // POST /api/v1/admin/bulk/enroll-division — { divisionId, semester }
    [HttpPost("enroll-division")]
    public async Task<IActionResult> EnrollDivision([FromBody] BulkEnrollDivisionRequest request, CancellationToken ct)
    {
        var guard = Admin(); if (guard is not null) return guard;
        return Ok(await service.EnrollDivisionAsync(request.DivisionId, request.Semester, ct));
    }

    private IActionResult? Admin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return IsStaff ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record BulkGraduateRequest([Required] long[] StudentIds, DateOnly GraduatedOn, [Required, StringLength(500, MinimumLength = 3)] string Reason);
public sealed record BulkEnrollDivisionRequest([Range(1, int.MaxValue)] int DivisionId, [Range(1, 2)] int Semester);
