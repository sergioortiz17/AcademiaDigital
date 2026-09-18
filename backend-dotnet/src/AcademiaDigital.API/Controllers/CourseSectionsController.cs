using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/course-sections")]
public sealed class CourseSectionsController(
    GetCourseSectionsQueryHandler listHandler,
    GetCourseSectionByIdQueryHandler getHandler,
    CreateCourseSectionCommandHandler createHandler,
    UpdateCourseSectionCommandHandler updateHandler,
    DeactivateCourseSectionCommandHandler deactivateHandler) : ApiControllerBase
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
            new GetCourseSectionsQuery(academicYear, semester, isVacant, includeInactive), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await getHandler.Handle(new GetCourseSectionByIdQuery(id), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveCourseSectionRequest request,
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
        [FromBody] SaveCourseSectionRequest request,
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
            new DeactivateCourseSectionCommand(id, CurrentUserId!.Value, reason), ct);
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

public sealed record SaveCourseSectionRequest(
    [Range(1, int.MaxValue)] int CourseId,
    [Range(1, int.MaxValue)] int DivisionId,
    [Range(2000, 2100)] int AcademicYear,
    [Range(1, 2)] int Semester,
    PositionType PositionType,
    [Range(1, 1000)] int MaxStudents)
{
    public CreateCourseSectionCommand ToCreateCommand() => new(
        CourseId, DivisionId, AcademicYear, Semester, PositionType, MaxStudents);

    public UpdateCourseSectionCommand ToUpdateCommand(int id) => new(
        id, CourseId, DivisionId, AcademicYear, Semester, PositionType, MaxStudents);
}
