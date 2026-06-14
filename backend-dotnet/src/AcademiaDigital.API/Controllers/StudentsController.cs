using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.UseCases.Students;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/students")]
public class StudentsController(
    GetStudentsQueryHandler getStudentsHandler,
    GetStudentByIdQueryHandler getStudentByIdHandler,
    CreateStudentCommandHandler createStudentHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? careerId, CancellationToken ct)
        => Ok(await getStudentsHandler.Handle(new GetStudentsQuery(careerId), ct));

    [HttpGet("{studentId:long}")]
    public async Task<IActionResult> GetById(long studentId, CancellationToken ct)
    {
        try
        {
            return Ok(await getStudentByIdHandler.Handle(new GetStudentByIdQuery(studentId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFoundProblem(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await createStudentHandler.Handle(new CreateStudentCommand(request), ct);
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
}
