using AcademiaDigital.Application.Dtos;
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
    GetCareerStudyPlanGroupedQueryHandler groupedHandler) : ControllerBase
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

    private ObjectResult NotFoundProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);
}
