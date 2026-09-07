using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>
/// Endpoints "de mí mismo" para el alumno logueado: resuelven el Student desde el JWT
/// (CurrentUserId), para que el frontend no tenga que conocer su propio studentId.
/// Son READ-ONLY y reutilizan los MISMOS handlers/queries que el resto (no reimplementan nada,
/// no tocan la validación real de inscripción).
/// </summary>
[ApiController]
[Route("api/v1/students/me")]
public sealed class StudentSelfController(
    IStudentRepository studentRepository,
    GetEligibleCoursesForStudentQueryHandler eligibleCoursesHandler) : ApiControllerBase
{
    // GET /api/v1/students/me/eligible-courses?careerId=
    // Mismo cálculo que /students/{id}/eligible-courses; solo resuelve el studentId del alumno
    // autenticado. Sirve para que el formulario de inscripción no ofrezca materias inválidas.
    [HttpGet("eligible-courses")]
    public async Task<IActionResult> GetMyEligibleCourses([FromQuery] int? careerId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var student = await studentRepository.FindByUserIdAsync(CurrentUserId.Value, ct);
        if (student is null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "El usuario actual no es un alumno.", Status = StatusCodes.Status404NotFound });
        try
        {
            return Ok(await eligibleCoursesHandler.Handle(new GetEligibleCoursesForStudentQuery(student.Id, careerId), ct));
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
