using AcademiaDigital.API.Controllers.Requests;
using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.StudyPlanDiff;
using AcademiaDigital.Application.UseCases.StudyPlanImport;
using AcademiaDigital.Application.UseCases.StudyPlans;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/careers/{careerId:int}/study-plans")]
public class StudyPlansController(
    GetCareerStudyPlansQueryHandler getStudyPlansHandler,
    CreateStudyPlanCommandHandler createStudyPlanHandler,
    UpdateStudyPlanCommandHandler updateStudyPlanHandler,
    ActivateStudyPlanCommandHandler activateStudyPlanHandler,
    GetCareerStudyPlanGroupedQueryHandler groupedHandler,
    ImportStudyPlanFromCsvCommandHandler importHandler,
    PreviewStudyPlanDiffCommandHandler diffPreviewHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByCareer(int careerId, CancellationToken ct)
        => Ok(await getStudyPlansHandler.Handle(new GetCareerStudyPlansQuery(careerId), ct));

    [HttpGet("{studyPlanId:int}/courses-grouped")]
    public async Task<IActionResult> GetGrouped(int careerId, int studyPlanId, CancellationToken ct)
    {
        try
        {
            return Ok(await groupedHandler.Handle(new GetCareerStudyPlanGroupedQuery(careerId, studyPlanId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(int careerId, [FromBody] CreateStudyPlanRequest request, CancellationToken ct)
    {
        try
        {
            var result = await createStudyPlanHandler.Handle(new CreateStudyPlanCommand(careerId, request), ct);
            return CreatedAtAction(nameof(GetByCareer), new { careerId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPut("{studyPlanId:int}")]
    public async Task<IActionResult> Update(int careerId, int studyPlanId, [FromBody] CreateStudyPlanRequest request, CancellationToken ct)
    {
        try
        {
            await updateStudyPlanHandler.Handle(new UpdateStudyPlanCommand(careerId, studyPlanId, request), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost("{studyPlanId:int}/activate")]
    public async Task<IActionResult> Activate(int careerId, int studyPlanId, CancellationToken ct)
    {
        try
        {
            await activateStudyPlanHandler.Handle(new ActivateStudyPlanCommand(careerId, studyPlanId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost("import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(int careerId, [FromForm] ImportStudyPlanCsvRequest request, CancellationToken ct)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new ImportStudyPlanFromCsvCommand(
            careerId,
            request.Code,
            request.Name,
            request.VersionNumber,
            request.EffectiveFrom,
            request.EffectiveTo,
            stream);

        try
        {
            var result = await importHandler.Handle(command, ct);

            if (!result.Success)
                return BadRequest(new { success = false, errors = result.Errors });

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                studyPlanId = result.StudyPlanId,
                coursesCreated = result.CoursesCreated,
                prerequisitesCreated = result.PrerequisitesCreated
            });
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost("{studyPlanId:int}/diff-preview")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> DiffPreview(int careerId, int studyPlanId, [FromForm] DiffPreviewCsvRequest request, CancellationToken ct)
    {
        await using var stream = request.File.OpenReadStream();

        try
        {
            var result = await diffPreviewHandler.Handle(new PreviewStudyPlanDiffCommand(careerId, studyPlanId, stream), ct);

            if (!result.Success)
                return BadRequest(new { success = false, errors = result.Errors });

            return Ok(result.Diff);
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    private ObjectResult NotFoundProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);
}
