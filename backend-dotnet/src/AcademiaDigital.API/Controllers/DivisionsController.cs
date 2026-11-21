using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Application.UseCases.Students;
using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/careers/{careerId:int}/divisions")]
public class DivisionsController(
    IStudentManagementService service, AssignDivisionToStudentsHandler assignStudents) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int careerId, [FromQuery] int? academicYear, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.GetDivisionsAsync(careerId, academicYear, ct)); }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int careerId, int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = (await service.GetDivisionsAsync(careerId, null, ct)).SingleOrDefault(c => c.Id == id); return x is null ? NotFound() : Ok(x); }

    [HttpPost]
    public async Task<IActionResult> Create(int careerId, UpsertDivisionRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.SaveDivisionAsync(careerId, null, request, ct); return CreatedAtAction(nameof(Get), new { careerId, id = x.Id }, x); }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int careerId, int id, UpsertDivisionRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.SaveDivisionAsync(careerId, id, request, ct)); }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int careerId, int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; await service.DisableDivisionAsync(careerId, id, ct); return NoContent(); }

    // POST /api/v1/careers/{careerId}/divisions/{id}/students  { "studentIds": [1, 2], "reason": "..." }
    // Asigna varios alumnos a la división; devuelve los asignados y el motivo de cada uno que falló.
    [HttpPost("{id:int}/students")]
    public async Task<IActionResult> AssignStudents(int careerId, int id, AssignStudentsToDivisionRequest request, CancellationToken ct)
    {
        var g = Admin(); if (g is not null) return g;
        var result = await assignStudents.Handle(careerId, id, request.StudentIds, request.Reason, CurrentUserId!.Value, ct);
        return Ok(new { success = true, result.Assigned, result.Failed });
    }

    private IActionResult? Admin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record AssignStudentsToDivisionRequest([Required] long[] StudentIds, [MaxLength(500)] string? Reason);
