using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/students")]
public class StudentsController(
    GetStudentByIdQueryHandler getStudentByIdHandler,
    CreateStudentCommandHandler createStudentHandler,
    IStudentManagementService management) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? careerId,
        [FromQuery] StudentStatus? status, [FromQuery] int? academicYear, [FromQuery] int? commissionId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var guard = RequireAdmin(); if (guard is not null) return guard;
        return Ok(await management.SearchStudentsAsync(search, careerId, status, academicYear, commissionId, page, pageSize, ct));
    }

    [HttpGet("{studentId:long}")]
    public async Task<IActionResult> GetById(long studentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Admin && !await management.IsOwnerAsync(studentId, CurrentUserId.Value, ct))
            return StatusCode(StatusCodes.Status403Forbidden);
        try
        {
            return Ok(await getStudentByIdHandler.Handle(new GetStudentByIdQuery(studentId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPut("{studentId:long}")]
    public async Task<IActionResult> Update(long studentId, [FromBody] UpdateStudentRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard is not null) return guard;
        return Ok(await management.UpdateStudentAsync(studentId, request, ct));
    }

    [HttpGet("{studentId:long}/careers")]
    public async Task<IActionResult> GetCareers(long studentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Admin && !await management.IsOwnerAsync(studentId, CurrentUserId.Value, ct))
            return StatusCode(StatusCodes.Status403Forbidden);
        try { return Ok(await management.GetStudentCareersAsync(studentId, ct)); }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost("{studentId:long}/careers")]
    public async Task<IActionResult> AddCareer(long studentId, [FromBody] AddStudentCareerRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard is not null) return guard;
        try
        {
            var result = await management.AddStudentCareerAsync(studentId, request, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictProblem(ex.Message); }
    }

    [HttpDelete("{studentId:long}")]
    public async Task<IActionResult> Delete(long studentId, [FromBody] DeleteStudentRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard is not null) return guard;
        await management.SoftDeleteStudentAsync(studentId, request.Reason, CurrentUserId!.Value, ct);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard is not null) return guard;
        try
        {
            var result = await createStudentHandler.Handle(new CreateStudentCommand(request, CurrentUserId!.Value), ct);
            return CreatedAtAction(nameof(GetById), new { studentId = result.Id }, result);
        }
        catch (ArgumentException ex) { return BadRequestProblem(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictProblem(ex.Message); }
    }

    private ObjectResult BadRequestProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
    private ObjectResult NotFoundProblem(string detail) => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ConflictProblem(string detail)
        => Conflict(new ProblemDetails { Title = "Conflict", Detail = detail, Status = StatusCodes.Status409Conflict });

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
}
