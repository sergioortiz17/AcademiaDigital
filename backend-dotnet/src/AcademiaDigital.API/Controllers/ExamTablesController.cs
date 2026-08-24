using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/exam-tables")]
public sealed class ExamTablesController(
    GetExamTablesQueryHandler listHandler,
    GetExamTableQueryHandler getHandler,
    CreateExamTableCommandHandler createHandler,
    RegisterForExamCommandHandler registerHandler,
    StartExamGradingCommandHandler startGradingHandler,
    SaveExamResultsCommandHandler resultsHandler,
    PublishExamTableCommandHandler publishHandler,
    ReopenExamTableCommandHandler reopenHandler,
    GetMyExamTablesQueryHandler myTablesHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? academicYear, [FromQuery] int? courseId, CancellationToken ct = default)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await listHandler.Handle(new GetExamTablesQuery(
            academicYear, courseId, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Alumno) return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await myTablesHandler.Handle(new GetMyExamTablesQuery(CurrentUserId.Value), ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await getHandler.Handle(new GetExamTableQuery(
            id, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(100, MinimumLength = 8)] string idempotencyKey,
        [FromBody] CreateExamTableRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await createHandler.Handle(new CreateExamTableCommand(
            idempotencyKey, request.CourseId, request.AcademicYear, request.CallNumber,
            request.ExamDateUtc, request.RegistrationDeadlineUtc, request.Location,
            request.Tribunal.Select(item => new ExamTribunalInput(item.TeacherId, item.Role)).ToArray(),
            CurrentUserId!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("{id:long}/registrations")]
    public async Task<IActionResult> Register(long id, [FromBody] RegisterForExamRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole is not (UserRole.Admin or UserRole.Alumno))
            return StatusCode(StatusCodes.Status403Forbidden);
        var created = await registerHandler.Handle(new RegisterForExamCommand(
            id, request.EnrollmentId, CurrentUserId.Value, CurrentUserRole == UserRole.Admin), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("{id:long}/start-grading")]
    public async Task<IActionResult> StartGrading(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await startGradingHandler.Handle(new StartExamGradingCommand(id, CurrentUserId!.Value), ct));
    }

    [HttpPut("{id:long}/results")]
    public async Task<IActionResult> SaveResults(long id, [FromBody] SaveExamResultsRequest request, CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await resultsHandler.Handle(new SaveExamResultsCommand(
            id,
            request.Results.Select(item => new ExamResultInput(
                item.RegistrationId, item.Outcome, item.Grade, item.Notes)).ToArray(),
            CurrentUserId!.Value,
            CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost("{id:long}/publish")]
    public async Task<IActionResult> Publish(long id, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await publishHandler.Handle(new PublishExamTableCommand(id, CurrentUserId!.Value), ct));
    }

    [HttpPost("{id:long}/reopen")]
    public async Task<IActionResult> Reopen(long id, [FromBody] ReopenExamTableRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reopenHandler.Handle(new ReopenExamTableCommand(
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

public sealed record CreateExamTableRequest(
    [Range(1, int.MaxValue)] int CourseId,
    [Range(2000, 2200)] int AcademicYear,
    [Range(1, 10)] int CallNumber,
    DateTime ExamDateUtc,
    DateTime RegistrationDeadlineUtc,
    [Required, StringLength(200, MinimumLength = 1)] string Location,
    [Required, MinLength(2), MaxLength(5)] IReadOnlyList<ExamTribunalMemberRequest> Tribunal);
public sealed record ExamTribunalMemberRequest(
    [Range(1, long.MaxValue)] long TeacherId,
    ExamTribunalRole Role);
public sealed record RegisterForExamRequest([Range(1, long.MaxValue)] long EnrollmentId);
public sealed record SaveExamResultsRequest([Required, MinLength(1)] IReadOnlyList<SaveExamResultRequest> Results);
public sealed record SaveExamResultRequest(
    [Range(1, long.MaxValue)] long RegistrationId,
    ExamResultOutcome Outcome,
    [Range(typeof(decimal), "0", "10")] decimal? Grade,
    [StringLength(500)] string? Notes);
public sealed record ReopenExamTableRequest(
    [Required, StringLength(1000, MinimumLength = 3)] string Reason);
