using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.Prerequisites;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/study-plans/{studyPlanId:int}/courses/{courseId:int}/prerequisites")]
public class PrerequisitesController(
    GetCoursePrerequisitesQueryHandler getPrerequisitesHandler,
    AddCoursePrerequisiteCommandHandler addPrerequisiteHandler,
    RemoveCoursePrerequisiteCommandHandler removePrerequisiteHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int studyPlanId, int courseId, CancellationToken ct)
        => Ok(await getPrerequisitesHandler.Handle(new GetCoursePrerequisitesQuery(studyPlanId, courseId), ct));

    [HttpPost]
    public async Task<IActionResult> Add(int studyPlanId, int courseId, [FromBody] AddCoursePrerequisiteRequest request, CancellationToken ct)
    {
        try
        {
            var result = await addPrerequisiteHandler.Handle(new AddCoursePrerequisiteCommand(studyPlanId, courseId, request), ct);
            return CreatedAtAction(nameof(Get), new { studyPlanId, courseId }, result);
        }
        catch (ArgumentException ex) { return BadRequestProblem(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictProblem(ex.Message); }
    }

    [HttpDelete("{prerequisiteCourseId:int}")]
    public async Task<IActionResult> Delete(int studyPlanId, int courseId, int prerequisiteCourseId, CancellationToken ct)
    {
        await removePrerequisiteHandler.Handle(new RemoveCoursePrerequisiteCommand(studyPlanId, courseId, prerequisiteCourseId), ct);
        return NoContent();
    }

    private ObjectResult BadRequestProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
    private ObjectResult NotFoundProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ConflictProblem(string detail)
        => Conflict(new ProblemDetails { Title = "Conflict", Detail = detail, Status = StatusCodes.Status409Conflict });
}
