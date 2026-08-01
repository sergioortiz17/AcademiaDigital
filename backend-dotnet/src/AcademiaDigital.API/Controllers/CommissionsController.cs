using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/careers/{careerId:int}/commissions")]
public class CommissionsController(IStudentManagementService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int careerId, [FromQuery] int? academicYear, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.GetCommissionsAsync(careerId, academicYear, ct)); }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int careerId, int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = (await service.GetCommissionsAsync(careerId, null, ct)).SingleOrDefault(c => c.Id == id); return x is null ? NotFound() : Ok(x); }

    [HttpPost]
    public async Task<IActionResult> Create(int careerId, UpsertCommissionRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.SaveCommissionAsync(careerId, null, request, ct); return CreatedAtAction(nameof(Get), new { careerId, id = x.Id }, x); }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int careerId, int id, UpsertCommissionRequest request, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.SaveCommissionAsync(careerId, id, request, ct)); }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int careerId, int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; await service.DisableCommissionAsync(careerId, id, ct); return NoContent(); }

    private IActionResult? Admin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
}
