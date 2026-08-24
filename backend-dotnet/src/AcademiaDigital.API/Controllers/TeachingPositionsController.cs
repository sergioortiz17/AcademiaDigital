using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/teaching-positions")]
public sealed class TeachingPositionsController(
    GetTeachingPositionsQueryHandler listHandler,
    GetTeachingPositionByIdQueryHandler getHandler,
    CreateTeachingPositionCommandHandler createHandler,
    UpdateTeachingPositionCommandHandler updateHandler,
    DeactivateTeachingPositionCommandHandler deactivateHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? academicYear,
        [FromQuery] int? semester,
        [FromQuery] bool? isVacant,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await listHandler.Handle(
            new GetTeachingPositionsQuery(academicYear, semester, isVacant, includeInactive), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await getHandler.Handle(new GetTeachingPositionByIdQuery(id), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveTeachingPositionRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await createHandler.Handle(request.ToCreateCommand(), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveTeachingPositionRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await updateHandler.Handle(request.ToUpdateCommand(id), ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(
        int id,
        [FromQuery, Required, StringLength(500, MinimumLength = 3)] string reason,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        await deactivateHandler.Handle(
            new DeactivateTeachingPositionCommand(id, CurrentUserId!.Value, reason), ct);
        return NoContent();
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record SaveTeachingPositionRequest(
    [Range(1, int.MaxValue)] int CourseId,
    [Range(1, int.MaxValue)] int CommissionId,
    [Range(2000, 2100)] int AcademicYear,
    [Range(1, 2)] int Semester,
    PositionType PositionType,
    [Range(1, 1000)] int MaxStudents)
{
    public CreateTeachingPositionCommand ToCreateCommand() => new(
        CourseId, CommissionId, AcademicYear, Semester, PositionType, MaxStudents);

    public UpdateTeachingPositionCommand ToUpdateCommand(int id) => new(
        id, CourseId, CommissionId, AcademicYear, Semester, PositionType, MaxStudents);
}
