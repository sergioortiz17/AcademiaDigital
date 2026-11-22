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
    GetMyExamTablesQueryHandler myTablesHandler,
    AcademiaDigital.Application.Services.IExamSchedulePlanner schedulePlanner) : ApiControllerBase
{
    protected override Permission? Area => Permission.ManageExams;

    // POST /api/v1/exam-tables/schedule-proposal — propone fechas y tribunal; no crea mesas.
    [HttpPost("schedule-proposal")]
    public async Task<IActionResult> ProposeSchedule([FromBody] ExamScheduleProposalRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (!IsStaff) return StatusCode(StatusCodes.Status403Forbidden);
        var holidays = Enumerable.Range(request.From.Year, request.To.Year - request.From.Year + 1)
            .SelectMany(year => AcademiaDigital.API.Helpers.ArgentineHolidays.GetForYear(year).Select(h => h.Date));
        var days = AcademiaDigital.Domain.Services.ExamScheduler.BusinessDays(request.From, request.To,
            holidays.Concat(request.BlockedDates ?? []));
        return Ok(await schedulePlanner.ProposeAsync(request.CareerId, request.AcademicYear, days, request.YearNumbers, ct));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? academicYear, [FromQuery] int? courseId, CancellationToken ct = default)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await listHandler.Handle(new GetExamTablesQuery(
            academicYear, courseId, CurrentUserId!.Value, IsStaff), ct));
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
            id, CurrentUserId!.Value, IsStaff), ct));
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
        if (!(IsStaff || CurrentUserRole == UserRole.Alumno))
            return StatusCode(StatusCodes.Status403Forbidden);
        var created = await registerHandler.Handle(new RegisterForExamCommand(
            id, request.EnrollmentId, CurrentUserId.Value, IsStaff), ct);
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
            IsStaff), ct));
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
        return IsStaff || CurrentUserRole == UserRole.Profesor
            ? null
            : StatusCode(StatusCodes.Status403Forbidden);
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return IsStaff
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
public sealed record ExamScheduleProposalRequest(
    [Range(1, int.MaxValue)] int CareerId,
    [Range(2000, 2200)] int AcademicYear,
    DateOnly From,
    DateOnly To,
    int[]? YearNumbers,
    DateOnly[]? BlockedDates);
