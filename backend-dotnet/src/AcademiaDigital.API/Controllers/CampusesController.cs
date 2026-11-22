using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>Escuelas (sedes) del instituto. Las ve el personal; las administra quien gestiona carreras.</summary>
[Route("api/v1/campuses")]
public class CampusesController(IStudentManagementService service) : ApiControllerBase
{
    protected override Permission? Area => Permission.ManageCareers;

    // GET /api/v1/campuses?includeInactive=true
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct)
    { var g = Staff(); if (g is not null) return g; return Ok(await service.GetCampusesAsync(includeInactive, ct)); }

    [HttpPost]
    public async Task<IActionResult> Create(UpsertCampusRequest request, CancellationToken ct)
    { var g = Staff(); if (g is not null) return g; return StatusCode(StatusCodes.Status201Created, await service.SaveCampusAsync(null, request, ct)); }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertCampusRequest request, CancellationToken ct)
    { var g = Staff(); if (g is not null) return g; return Ok(await service.SaveCampusAsync(id, request, ct)); }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    { var g = Staff(); if (g is not null) return g; await service.DisableCampusAsync(id, ct); return NoContent(); }

    private IActionResult? Staff()
    {
        if (CurrentUserId is null) return Unauthorized();
        return IsStaff ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
}
