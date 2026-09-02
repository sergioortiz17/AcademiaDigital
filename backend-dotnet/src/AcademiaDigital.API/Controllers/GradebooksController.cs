using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/gradebooks")]
public sealed class GradebooksController(
    GetGradebooksQueryHandler listHandler,
    GetGradebookQueryHandler getHandler,
    CreateGradebookCommandHandler createHandler,
    SaveGradeEntriesCommandHandler gradesHandler,
    SubmitGradebookCommandHandler submitHandler,
    ApproveGradebookCommandHandler approveHandler,
    PublishGradebookCommandHandler publishHandler,
    CloseGradebookCommandHandler closeHandler,
    ReopenGradebookCommandHandler reopenHandler,
    GetMyGradesQueryHandler myGradesHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? academicYear,
        [FromQuery] int? courseId,
        [FromQuery] int? commissionId,
        CancellationToken ct = default)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await listHandler.Handle(new GetGradebooksQuery(
            academicYear, courseId, commissionId,
            CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyGrades([FromQuery] int? courseId, CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Alumno) return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await myGradesHandler.Handle(new GetMyGradesQuery(CurrentUserId.Value, courseId), ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await getHandler.Handle(new GetGradebookQuery(
            id, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(100, MinimumLength = 8)] string idempotencyKey,
        [FromBody] CreateGradebookRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        var created = await createHandler.Handle(new CreateGradebookCommand(
            idempotencyKey,
            request.TeachingPositionId,
            request.Evaluations.Select(item => new GradebookEvaluationInput(
                item.Name, item.WeightPercentage, item.MaximumScore)).ToArray(),
            CurrentUserId!.Value,
            CurrentUserRole == UserRole.Admin), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:long}/grades")]
    public async Task<IActionResult> SaveGrades(long id, [FromBody] SaveGradeEntriesRequest request, CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await gradesHandler.Handle(new SaveGradeEntriesCommand(
            id,
            request.Grades.Select(item => new GradeEntryInput(
                item.EvaluationId, item.EnrollmentId, item.Score, item.Notes)).ToArray(),
            CurrentUserId!.Value,
            CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost("{id:long}/submit")]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await submitHandler.Handle(new SubmitGradebookCommand(
            id, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await approveHandler.Handle(new ApproveGradebookCommand(id, CurrentUserId!.Value), ct));
    }

    [HttpPost("{id:long}/publish")]
    public async Task<IActionResult> Publish(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await publishHandler.Handle(new PublishGradebookCommand(id, CurrentUserId!.Value), ct));
    }

    [HttpPost("{id:long}/close")]
    public async Task<IActionResult> Close(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await closeHandler.Handle(new CloseGradebookCommand(id, CurrentUserId!.Value), ct));
    }

    [HttpPost("{id:long}/reopen")]
    public async Task<IActionResult> Reopen(long id, [FromBody] ReopenGradebookRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reopenHandler.Handle(new ReopenGradebookCommand(
            id, request.Reason, CurrentUserId!.Value), ct));
    }

    private IActionResult? RequireAdminOrProfessor()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole is UserRole.Admin or UserRole.Profesor
            ? null
            : StatusCode(StatusCodes.Status403Forbidden);
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record CreateGradebookRequest(
    [Range(1, int.MaxValue)] int TeachingPositionId,
    [Required, MinLength(1), MaxLength(20)] IReadOnlyList<GradebookEvaluationRequest> Evaluations);
public sealed record GradebookEvaluationRequest(
    [Required, StringLength(150, MinimumLength = 1)] string Name,
    [Range(typeof(decimal), "0.01", "100")] decimal WeightPercentage,
    [Range(typeof(decimal), "0.01", "100")] decimal MaximumScore = 10m);
public sealed record SaveGradeEntriesRequest(
    [Required, MinLength(1)] IReadOnlyList<SaveGradeEntryRequest> Grades);
public sealed record SaveGradeEntryRequest(
    [Range(1, long.MaxValue)] long EvaluationId,
    [Range(1, long.MaxValue)] long EnrollmentId,
    [Range(typeof(decimal), "0", "100")] decimal Score,
    [StringLength(500)] string? Notes);
public sealed record ReopenGradebookRequest(
    [Required, StringLength(1000, MinimumLength = 3)] string Reason);
