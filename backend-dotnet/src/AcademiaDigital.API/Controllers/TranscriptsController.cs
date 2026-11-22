using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>Analíticos: Administración revisa y completa libro y folio antes de emitirlo.</summary>
[Route("api/v1/admin")]
public sealed class TranscriptsController(ITranscriptService transcripts) : ApiControllerBase
{
    protected override Permission? Area => Permission.ManageStudents;

    [HttpGet("transcripts/{studentId:long}")]
    public async Task<IActionResult> Get(long studentId, CancellationToken ct)
    {
        var guard = Admin(); if (guard is not null) return guard;
        return Ok(await transcripts.GetAsync(studentId, ct));
    }

    // PUT /api/v1/admin/transcripts/{studentId}/registry — libro matriz y folio del alumno
    [HttpPut("transcripts/{studentId:long}/registry")]
    public async Task<IActionResult> SetRegistry(long studentId, [FromBody] RegistryRequest request, CancellationToken ct)
    {
        var guard = Admin(); if (guard is not null) return guard;
        await transcripts.SetRegistryAsync(studentId, request.Book, request.Folio, ct);
        return NoContent();
    }

    // PUT /api/v1/admin/exam-tables/{id}/act — libro y folio del acta de la mesa
    [HttpPut("exam-tables/{examTableId:long}/act")]
    public async Task<IActionResult> SetAct(long examTableId, [FromBody] RegistryRequest request, CancellationToken ct)
    {
        var guard = Admin(); if (guard is not null) return guard;
        await transcripts.SetExamActAsync(examTableId, request.Book, request.Folio, ct);
        return NoContent();
    }

    private IActionResult? Admin()
    {
        if (CurrentUserId is null) return Unauthorized();
        return IsStaff ? null : StatusCode(StatusCodes.Status403Forbidden);
    }
}

public sealed record RegistryRequest(string? Book, string? Folio);
