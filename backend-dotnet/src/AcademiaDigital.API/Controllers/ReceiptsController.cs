using System.ComponentModel.DataAnnotations;
using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Receipts;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

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
        return Ok(await historyHandler.Handle(new(
            CurrentUserId!.Value, true, studentId), ct));
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> Get(Guid publicId, CancellationToken ct)
    {
        var guard = RequireAdminOrStudent();
        if (guard is not null) return guard;
        return Ok(await receiptHandler.Handle(new(
            publicId, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct));
    }

    [HttpPost("{publicId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid publicId, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await workflow.RetryAsync(publicId, ct));
    }

    [HttpGet("{publicId:guid}/download")]
    public async Task<IActionResult> Download(Guid publicId, CancellationToken ct)
    {
        var guard = RequireAdminOrStudent();
        if (guard is not null) return guard;
        var file = await downloadHandler.Handle(new(
            publicId, CurrentUserId!.Value, CurrentUserRole == UserRole.Admin), ct);
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

[Route("api/v1/students/me/receipts")]
public sealed class MyReceiptsController(GetReceiptsQueryHandler historyHandler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Alumno)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Only students can query their own receipts."));
        return Ok(await historyHandler.Handle(new(CurrentUserId.Value, false), ct));
    }
}
