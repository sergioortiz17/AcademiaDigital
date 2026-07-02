using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/enrollments")]
public class EnrollmentsController(
    EnrollmentPeriodFacade periods,
    CreateEnrollmentCommandHandler createEnrollmentHandler,
    IStudentRepository studentRepository) : ApiControllerBase
{
    // GET /api/v1/enrollments/periods
    [HttpGet("periods")]
    public async Task<IActionResult> GetAllPeriods(CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var result = await periods.GetAllAsync(ct);
        return Ok(new { success = true, data = result });
    }

    // GET /api/v1/enrollments/periods/active?careerId=1
    [HttpGet("periods/active")]
    public async Task<IActionResult> GetActivePeriod([FromQuery] int careerId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var result = await periods.GetActiveAsync(careerId, ct);
        return Ok(new { success = true, data = result });
    }

    // GET /api/v1/enrollments/periods/{id}/students
    [HttpGet("periods/{id:int}/students")]
    public async Task<IActionResult> GetEnrolledStudents(int id, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var (total, students) = await periods.GetStudentsAsync(id, ct);
        return Ok(new { success = true, total, data = students });
    }

    // POST /api/v1/enrollments/periods
    [HttpPost("periods")]
    public async Task<IActionResult> OpenPeriod([FromBody] OpenPeriodRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var command = new OpenEnrollmentPeriodCommand(
            request.CareerId,
            request.StudyPlanId,
            request.AcademicYear,
            request.Semester,
            request.QuotasMorning,
            request.QuotasAfternoon,
            request.QuotasEvening);
        var result = await periods.OpenAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, data = result });
    }

    // PUT /api/v1/enrollments/periods/{id}/quotas
    [HttpPut("periods/{id:int}/quotas")]
    public async Task<IActionResult> UpdateQuotas(int id, [FromBody] UpdateQuotasRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        var command = new UpdatePeriodQuotasCommand(id, request.QuotasMorning, request.QuotasAfternoon, request.QuotasEvening);
        var result = await periods.UpdateQuotasAsync(command, ct);
        return Ok(new { success = true, data = result });
    }

    // PUT /api/v1/enrollments/periods/{id}/close
    [HttpPut("periods/{id:int}/close")]
    public async Task<IActionResult> ClosePeriod(int id, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        await periods.CloseAsync(id, ct);
        return Ok(new { success = true, msg = "Período de inscripción cerrado." });
    }

    // DELETE /api/v1/enrollments/periods/{id}/students/{studentId}
    [HttpDelete("periods/{id:int}/students/{studentId:long}")]
    public async Task<IActionResult> RemoveStudent(int id, long studentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        await periods.RemoveStudentAsync(id, studentId, ct);
        return Ok(new { success = true, msg = "Inscripción eliminada correctamente." });
    }

    // POST /api/v1/enrollments
    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();

        var student = await studentRepository.FindByUserIdAsync(CurrentUserId.Value, ct)
            ?? throw new KeyNotFoundException("Student profile not found for current user.");

        var command = new CreateEnrollmentCommand(
            student.Id,
            request.EnrollmentPeriodId,
            request.Shift,
            request.StudyPlanCourseIds);

        await createEnrollmentHandler.Handle(command, ct);
        return StatusCode(StatusCodes.Status201Created, new { success = true, msg = "Inscripción realizada correctamente." });
    }
}

public record OpenPeriodRequest(
    [Required] int CareerId,
    [Required] int StudyPlanId,
    [Required] int AcademicYear,
    [Required] int Semester,
    [Required][Range(0, 9999)] int QuotasMorning,
    [Required][Range(0, 9999)] int QuotasAfternoon,
    [Required][Range(0, 9999)] int QuotasEvening);

public record UpdateQuotasRequest(
    [Required][Range(0, 9999)] int QuotasMorning,
    [Required][Range(0, 9999)] int QuotasAfternoon,
    [Required][Range(0, 9999)] int QuotasEvening);

public record EnrollRequest(
    [Required] int EnrollmentPeriodId,
    [Required] string Shift,
    [Required] IReadOnlyList<int> StudyPlanCourseIds);
