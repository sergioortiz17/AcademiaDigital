using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Application.UseCases.Attendance;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/attendance")]
public sealed class AttendanceController(
    GetAttendanceSessionsQueryHandler listHandler,
    GetAttendanceSessionQueryHandler getHandler,
    CreateAttendanceSessionCommandHandler createHandler,
    SaveAttendanceRecordsCommandHandler saveRecordsHandler,
    CloseAttendanceSessionCommandHandler closeHandler,
    ReopenAttendanceSessionCommandHandler reopenHandler,
    JustifyAttendanceRecordCommandHandler justifyHandler,
    GetStudentAttendanceSummaryQueryHandler studentSummaryHandler,
    GetMyAttendanceSummaryQueryHandler mySummaryHandler,
    ExportAttendanceSessionQueryHandler exportHandler) : ApiControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> ListSessions(
        [FromQuery] int? academicYear,
        [FromQuery] int? courseId,
        [FromQuery] int? commissionId,
        CancellationToken ct = default)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await listHandler.Handle(new GetAttendanceSessionsQuery(
            academicYear, courseId, commissionId,
            CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpGet("sessions/{id:long}")]
    public async Task<IActionResult> GetSession(long id, CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await getHandler.Handle(new GetAttendanceSessionQuery(
            id, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession(
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(100, MinimumLength = 8)] string idempotencyKey,
        [FromBody] CreateAttendanceSessionRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        var created = await createHandler.Handle(new CreateAttendanceSessionCommand(
            idempotencyKey,
            request.TeachingPositionId,
            request.SessionDate,
            request.StartTime,
            request.EndTime,
            request.Scope,
            request.Units,
            CurrentUserId!.Value,
            CurrentUserRole == UserRole.Admin), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("sessions/{id:long}/records")]
    public async Task<IActionResult> SaveRecords(
        long id,
        [FromBody] SaveAttendanceRecordsRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await saveRecordsHandler.Handle(new SaveAttendanceRecordsCommand(
            id,
            request.Records.Select(record => new AttendanceRecordInput(
                record.EnrollmentId, record.Status, record.Notes)).ToArray(),
            CurrentUserId!.Value,
            CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost("sessions/{id:long}/close")]
    public async Task<IActionResult> CloseSession(long id, CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await closeHandler.Handle(new CloseAttendanceSessionCommand(
            id, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost("sessions/{id:long}/reopen")]
    public async Task<IActionResult> ReopenSession(
        long id,
        [FromBody] ReopenAttendanceSessionRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reopenHandler.Handle(new ReopenAttendanceSessionCommand(
            id, request.Reason, CurrentUserId!.Value), ct));
    }

    [HttpPost("records/{recordId:long}/justifications")]
    public async Task<IActionResult> JustifyRecord(
        long recordId,
        [FromBody] JustifyAttendanceRecordRequest request,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await justifyHandler.Handle(new JustifyAttendanceRecordCommand(
            recordId,
            request.Category,
            request.Reason,
            request.EvidenceUrl,
            CurrentUserId!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("students/{studentId:long}/summary")]
    public async Task<IActionResult> GetStudentSummary(
        long studentId,
        [FromQuery] int? courseId,
        [FromQuery] int? commissionId,
        CancellationToken ct = default)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        return Ok(await studentSummaryHandler.Handle(new GetStudentAttendanceSummaryQuery(
            studentId, courseId, commissionId,
            CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpGet("me/summary")]
    public async Task<IActionResult> GetMySummary(
        [FromQuery] int? courseId,
        [FromQuery] int? commissionId,
        CancellationToken ct = default)
    {
        if (CurrentUserId is null) return Unauthorized();
        if (CurrentUserRole != UserRole.Alumno) return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await mySummaryHandler.Handle(new GetMyAttendanceSummaryQuery(
            CurrentUserId.Value, courseId, commissionId), ct));
    }

    [HttpGet("sessions/{id:long}/export")]
    public async Task<IActionResult> ExportSession(
        long id,
        [FromQuery, Required] string format,
        CancellationToken ct)
    {
        var guard = RequireAdminOrProfessor();
        if (guard is not null) return guard;
        var report = await exportHandler.Handle(new ExportAttendanceSessionQuery(
            id, format, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct);
        return File(report.Content, report.ContentType, report.FileName);
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

public sealed record CreateAttendanceSessionRequest(
    [Range(1, int.MaxValue)] int TeachingPositionId,
    DateOnly SessionDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    AttendanceScope Scope,
    [Range(1, 12)] int Units = 1);

public sealed record SaveAttendanceRecordsRequest(
    [Required, MinLength(1)] IReadOnlyList<SaveAttendanceRecordRequest> Records);

public sealed record SaveAttendanceRecordRequest(
    [Range(1, long.MaxValue)] long EnrollmentId,
    AttendanceRecordStatus Status,
    [StringLength(500)] string? Notes);

public sealed record ReopenAttendanceSessionRequest(
    [Required, StringLength(1000, MinimumLength = 3)] string Reason);

public sealed record JustifyAttendanceRecordRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Category,
    [Required, StringLength(1000, MinimumLength = 3)] string Reason,
    [StringLength(1000)] string? EvidenceUrl);
