using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Finance.API.Models;
using AcademiaDigital.Finance.Application.UseCases.Receipts;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.Finance.API.Controllers;

[Route("api/v1/receipts")]
public sealed class ReceiptsController(
    GetReceiptsQueryHandler historyHandler,
    GetReceiptQueryHandler receiptHandler,
    ReceiptWorkflowService workflow,
    DownloadReceiptQueryHandler downloadHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByStudent(
        [FromQuery, Range(1, long.MaxValue)] long studentId,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await historyHandler.Handle(new(true, studentId), ct));
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> Get(Guid publicId, [FromQuery] long? studentId, CancellationToken ct)
    {
        var guard = RequireAdminOrStudent();
        if (guard is not null) return guard;
        var isAdmin = CurrentUserRole == UserRole.Admin;
        return Ok(await receiptHandler.Handle(new(publicId, isAdmin, isAdmin ? null : studentId), ct));
    }

    [HttpPost("{publicId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid publicId, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await workflow.RetryAsync(publicId, ct));
    }

    [HttpGet("{publicId:guid}/download")]
    public async Task<IActionResult> Download(Guid publicId, [FromQuery] long? studentId, CancellationToken ct)
    {
        var guard = RequireAdminOrStudent();
        if (guard is not null) return guard;
        var isAdmin = CurrentUserRole == UserRole.Admin;
        var file = await downloadHandler.Handle(new(publicId, isAdmin, isAdmin ? null : studentId), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
    }

    private IActionResult? RequireAdminOrStudent()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole is UserRole.Admin or UserRole.Alumno
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin or student only."));
    }
}
