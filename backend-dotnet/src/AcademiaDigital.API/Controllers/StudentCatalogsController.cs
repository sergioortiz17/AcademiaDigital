using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1")]
public class StudentCatalogsController(IStudentManagementService service) : ApiControllerBase
{
    [HttpGet("document-requirements")]
    public async Task<IActionResult> Requirements([FromQuery] int? careerId, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.GetRequirementsAsync(careerId, ct)); }
    [HttpGet("document-requirements/{id:int}")]
    public async Task<IActionResult> Requirement(int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = (await service.GetRequirementsAsync(null, ct)).SingleOrDefault(r => r.Id == id); return x is null ? NotFound() : Ok(x); }
    [HttpPost("document-requirements")]
    public async Task<IActionResult> CreateRequirement(UpsertDocumentRequirementRequest r, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.SaveRequirementAsync(null, r, ct); return CreatedAtAction(nameof(Requirement), new { id = x.Id }, x); }
    [HttpPut("document-requirements/{id:int}")]
    public async Task<IActionResult> UpdateRequirement(int id, UpsertDocumentRequirementRequest r, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.SaveRequirementAsync(id, r, ct)); }
    [HttpDelete("document-requirements/{id:int}")]
    public async Task<IActionResult> DeleteRequirement(int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; await service.DisableRequirementAsync(id, ct); return NoContent(); }

    [HttpGet("scholarships")]
    public async Task<IActionResult> Scholarships(CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.GetScholarshipsAsync(ct)); }
    [HttpGet("scholarships/{id:int}")]
    public async Task<IActionResult> Scholarship(int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = (await service.GetScholarshipsAsync(ct)).SingleOrDefault(s => s.Id == id); return x is null ? NotFound() : Ok(x); }
    [HttpPost("scholarships")]
    public async Task<IActionResult> CreateScholarship(UpsertScholarshipRequest r, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.SaveScholarshipAsync(null, r, ct); return CreatedAtAction(nameof(Scholarship), new { id = x.Id }, x); }
    [HttpPut("scholarships/{id:int}")]
    public async Task<IActionResult> UpdateScholarship(int id, UpsertScholarshipRequest r, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.SaveScholarshipAsync(id, r, ct)); }
    [HttpDelete("scholarships/{id:int}")]
    public async Task<IActionResult> DeleteScholarship(int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; await service.DisableScholarshipAsync(id, ct); return NoContent(); }

    [HttpGet("student-custom-fields")]
    public async Task<IActionResult> CustomFields(CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.GetCustomFieldsAsync(ct)); }
    [HttpGet("student-custom-fields/{id:int}")]
    public async Task<IActionResult> CustomField(int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = (await service.GetCustomFieldsAsync(ct)).SingleOrDefault(f => f.Id == id); return x is null ? NotFound() : Ok(x); }
    [HttpPost("student-custom-fields")]
    public async Task<IActionResult> CreateCustomField(UpsertCustomFieldRequest r, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; var x = await service.SaveCustomFieldAsync(null, r, ct); return CreatedAtAction(nameof(CustomField), new { id = x.Id }, x); }
    [HttpPut("student-custom-fields/{id:int}")]
    public async Task<IActionResult> UpdateCustomField(int id, UpsertCustomFieldRequest r, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; return Ok(await service.SaveCustomFieldAsync(id, r, ct)); }
    [HttpDelete("student-custom-fields/{id:int}")]
    public async Task<IActionResult> DeleteCustomField(int id, CancellationToken ct)
    { var g = Admin(); if (g is not null) return g; await service.DisableCustomFieldAsync(id, ct); return NoContent(); }

    private IActionResult? Admin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
}
