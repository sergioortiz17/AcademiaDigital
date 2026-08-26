using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/students/{studentId:long}/rematriculations")]
public sealed class StudentRematriculationsController(
    CreateStudentRematriculationCommandHandler createHandler) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        long studentId,
        [FromBody] CreateStudentRematriculationRequest request,
        CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized();
        if (CurrentUserRole != UserRole.Admin)
            return StatusCode(StatusCodes.Status403Forbidden);

        var result = await createHandler.Handle(
            new CreateStudentRematriculationCommand(
                studentId,
                request.CareerId,
                request.StudyPlanId,
                request.CommissionId,
                request.AcademicYear,
                request.YearNumber,
                request.Notes,
                CurrentUserId.Value),
            ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

public sealed record CreateStudentRematriculationRequest(
    [Range(1, int.MaxValue)] int CareerId,
    [Range(1, int.MaxValue)] int StudyPlanId,
    [Range(1, int.MaxValue)] int CommissionId,
    [Range(2000, 2100)] int AcademicYear,
    [Range(1, 20)] int YearNumber,
    [StringLength(500)] string? Notes);
