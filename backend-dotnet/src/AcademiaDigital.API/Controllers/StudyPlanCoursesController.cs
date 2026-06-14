using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.StudyPlanCourses;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/study-plans/{studyPlanId:int}/courses")]
public class StudyPlanCoursesController(
    GetStudyPlanCoursesQueryHandler getCoursesHandler,
    AddCourseToStudyPlanCommandHandler addCourseHandler,
    UpdateStudyPlanCourseCommandHandler updateCourseHandler,
    RemoveCourseFromStudyPlanCommandHandler removeCourseHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByStudyPlan(int studyPlanId, CancellationToken ct)
        => Ok(await getCoursesHandler.Handle(new GetStudyPlanCoursesQuery(studyPlanId), ct));

    [HttpPost]
    public async Task<IActionResult> Add(int studyPlanId, [FromBody] AddCourseToStudyPlanRequest request, CancellationToken ct)
    {
        try
        {
            var result = await addCourseHandler.Handle(new AddCourseToStudyPlanCommand(studyPlanId, request), ct);
            return CreatedAtAction(nameof(GetByStudyPlan), new { studyPlanId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictProblem(ex.Message); }
    }

    [HttpPut("{studyPlanCourseId:int}")]
    public async Task<IActionResult> Update(int studyPlanId, int studyPlanCourseId, [FromBody] AddCourseToStudyPlanRequest request, CancellationToken ct)
    {
        try
        {
            await updateCourseHandler.Handle(new UpdateStudyPlanCourseCommand(studyPlanId, studyPlanCourseId, request), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpDelete("{studyPlanCourseId:int}")]
    public async Task<IActionResult> Delete(int studyPlanId, int studyPlanCourseId, CancellationToken ct)
    {
        try
        {
            await removeCourseHandler.Handle(new RemoveCourseFromStudyPlanCommand(studyPlanId, studyPlanCourseId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    private ObjectResult NotFoundProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ConflictProblem(string detail)
        => Conflict(new ProblemDetails { Title = "Conflict", Detail = detail, Status = StatusCodes.Status409Conflict });
}
