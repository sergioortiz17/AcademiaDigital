using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.Courses;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/careers/{careerId:int}/courses")]
public class CoursesController(
    GetCareerCoursesQueryHandler getCoursesHandler,
    CreateCourseCommandHandler createCourseHandler,
    UpdateCourseCommandHandler updateCourseHandler,
    DeleteCourseCommandHandler deleteCourseHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByCareer(int careerId, CancellationToken ct)
        => Ok(await getCoursesHandler.Handle(new GetCareerCoursesQuery(careerId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(int careerId, [FromBody] UpsertCourseRequest request, CancellationToken ct)
    {
        try
        {
            var result = await createCourseHandler.Handle(new CreateCourseCommand(careerId, request), ct);
            return CreatedAtAction(nameof(GetByCareer), new { careerId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictProblem(ex.Message); }
    }

    [HttpPut("{courseId:int}")]
    public async Task<IActionResult> Update(int careerId, int courseId, [FromBody] UpsertCourseRequest request, CancellationToken ct)
    {
        try
        {
            await updateCourseHandler.Handle(new UpdateCourseCommand(careerId, courseId, request), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpDelete("{courseId:int}")]
    public async Task<IActionResult> Delete(int careerId, int courseId, CancellationToken ct)
    {
        try
        {
            await deleteCourseHandler.Handle(new DeleteCourseCommand(careerId, courseId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    private ObjectResult NotFoundProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ConflictProblem(string detail)
        => Conflict(new ProblemDetails { Title = "Conflict", Detail = detail, Status = StatusCodes.Status409Conflict });
}
