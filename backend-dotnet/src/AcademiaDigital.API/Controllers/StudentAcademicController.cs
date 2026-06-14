using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.Students;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/students/{studentId:long}")]
public class StudentAcademicController(
    GetEligibleCoursesForStudentQueryHandler eligibleCoursesHandler,
    GetStudentAcademicProgressQueryHandler academicProgressHandler,
    AssignStudentStudyPlanCommandHandler assignStudyPlanHandler) : ControllerBase
{
    [HttpGet("eligible-courses")]
    public async Task<IActionResult> GetEligibleCourses(long studentId, CancellationToken ct)
    {
        try
        {
            return Ok(await eligibleCoursesHandler.Handle(new GetEligibleCoursesForStudentQuery(studentId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpGet("academic-progress")]
    public async Task<IActionResult> GetAcademicProgress(long studentId, CancellationToken ct)
    {
        try
        {
            return Ok(await academicProgressHandler.Handle(new GetStudentAcademicProgressQuery(studentId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost("study-plan")]
    public async Task<IActionResult> AssignStudyPlan(long studentId, [FromBody] AssignStudentStudyPlanRequest request, CancellationToken ct)
    {
        try
        {
            await assignStudyPlanHandler.Handle(new AssignStudentStudyPlanCommand(studentId, request), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictProblem(ex.Message); }
    }

    private ObjectResult NotFoundProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ConflictProblem(string detail)
        => Conflict(new ProblemDetails { Title = "Conflict", Detail = detail, Status = StatusCodes.Status409Conflict });
}
