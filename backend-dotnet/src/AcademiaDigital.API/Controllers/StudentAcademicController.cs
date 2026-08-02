using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/students/{studentId:long}")]
public class StudentAcademicController(
    GetEligibleCoursesForStudentQueryHandler eligibleCoursesHandler,
    GetStudentAcademicProgressQueryHandler academicProgressHandler,
    AssignStudentStudyPlanCommandHandler assignStudyPlanHandler,
    IStudentManagementService studentManagement) : ApiControllerBase
{
    [HttpGet("eligible-courses")]
    public async Task<IActionResult> GetEligibleCourses(long studentId, [FromQuery] int? careerId, CancellationToken ct)
    {
        var guard = await RequireReaderAsync(studentId, ct); if (guard is not null) return guard;
        try
        {
            return Ok(await eligibleCoursesHandler.Handle(new GetEligibleCoursesForStudentQuery(studentId, careerId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpGet("academic-progress")]
    public async Task<IActionResult> GetAcademicProgress(long studentId, [FromQuery] int? careerId, CancellationToken ct)
    {
        var guard = await RequireReaderAsync(studentId, ct); if (guard is not null) return guard;
        try
        {
            return Ok(await academicProgressHandler.Handle(new GetStudentAcademicProgressQuery(studentId, careerId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost("study-plan")]
    public async Task<IActionResult> AssignStudyPlan(long studentId, [FromBody] AssignStudentStudyPlanRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard is not null) return guard;
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

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin ? null : StatusCode(StatusCodes.Status403Forbidden);
    }

    private async Task<IActionResult?> RequireReaderAsync(long studentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole == UserRole.Admin || await studentManagement.IsOwnerAsync(studentId, CurrentUserId.Value, ct))
            return null;
        return StatusCode(StatusCodes.Status403Forbidden);
    }
}
