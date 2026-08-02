using AcademiaDigital.API.Controllers.Requests;
using AcademiaDigital.Application.UseCases.CareerImport;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/careers/import")]
public class CareerImportController(ImportCareerFromCsvCommandHandler handler) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import([FromForm] ImportCareerCsvRequest request, CancellationToken ct)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new ImportCareerFromCsvCommand(
            request.Name,
            request.Code,
            request.Description,
            request.TotalCredits,
            request.DurationYears,
            request.StudyPlanCode,
            request.StudyPlanName,
            request.VersionNumber,
            request.EffectiveFrom,
            stream);

        var result = await handler.Handle(command, ct);

        if (!result.Success)
            return BadRequest(new { success = false, errors = result.Errors });

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            careerId = result.CareerId,
            studyPlanId = result.StudyPlanId,
            coursesCreated = result.CoursesCreated,
            prerequisitesCreated = result.PrerequisitesCreated
        });
    }
}
