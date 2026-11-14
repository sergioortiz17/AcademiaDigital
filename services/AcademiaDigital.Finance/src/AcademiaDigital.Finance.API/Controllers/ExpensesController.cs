using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Finance.API.Models;
using AcademiaDigital.Finance.Application.UseCases.Expenses;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.Finance.API.Controllers;

[Route("api/v1/finance/expenses")]
public sealed class ExpensesController(
    GetExpenseCategoriesQueryHandler categoriesHandler,
    CreateExpenseCategoryCommandHandler createCategoryHandler,
    SetExpenseCategoryStatusCommandHandler categoryStatusHandler,
    RegisterExpenseCommandHandler registerHandler,
    ReverseExpenseCommandHandler reverseHandler,
    GetExpensesQueryHandler expensesHandler,
    DownloadExpenseAttachmentQueryHandler downloadHandler) : ApiControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var guard = RequireTreasury();
        return guard ?? Ok(await categoriesHandler.Handle(activeOnly, ct));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] ExpenseCategoryRequest request, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        var result = await createCategoryHandler.Handle(new(request.Code, request.Name, CurrentUserId!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("categories/{id:int}/active")]
    public async Task<IActionResult> SetCategoryStatus(int id, [FromBody] ExpenseCategoryStatusRequest request, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        return Ok(await categoryStatusHandler.Handle(new(id, request.IsActive), ct));
    }

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Register([FromForm] RegisterExpenseRequest request, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        byte[]? attachment = null;
        if (request.Attachment is not null)
        {
            if (request.Attachment.Length > 10 * 1024 * 1024) return BadRequest(ApiResponse.Fail("Attachment cannot exceed 10 MB."));
            await using var stream = new MemoryStream();
            await request.Attachment.CopyToAsync(stream, ct);
            attachment = stream.ToArray();
        }
        var result = await registerHandler.Handle(new(request.CategoryId, request.SupplierName, request.ExpenseDate,
            request.Amount, request.PurchaseNote, request.Observation, attachment, request.Attachment?.ContentType,
            request.Attachment?.FileName, CurrentUserId!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? categoryId,
        [FromQuery] string? supplier, CancellationToken ct)
    {
        var guard = RequireTreasury();
        return guard ?? Ok(await expensesHandler.Handle(new ExpenseFilter(from, to, categoryId, supplier), ct));
    }

    [HttpPost("{publicId:guid}/reverse")]
    public async Task<IActionResult> Reverse(Guid publicId, [FromBody] ReverseExpenseRequest request, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        return Ok(await reverseHandler.Handle(new(publicId, request.Reason, CurrentUserId!.Value), ct));
    }

    [HttpGet("{publicId:guid}/attachment")]
    public async Task<IActionResult> DownloadAttachment(Guid publicId, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        var file = await downloadHandler.Handle(publicId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private IActionResult? RequireTreasury()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole is UserRole.Admin or UserRole.Tesoreria
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Treasury access required."));
    }
}

public sealed record ExpenseCategoryRequest(
    [Required, StringLength(30, MinimumLength = 2)] string Code,
    [Required, StringLength(120, MinimumLength = 2)] string Name);
public sealed record ExpenseCategoryStatusRequest(bool IsActive);
public sealed class RegisterExpenseRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; init; }
    [Required, StringLength(200)] public string SupplierName { get; init; } = string.Empty;
    public DateOnly ExpenseDate { get; init; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] public decimal Amount { get; init; }
    [StringLength(100)] public string? PurchaseNote { get; init; }
    [StringLength(1000)] public string? Observation { get; init; }
    public IFormFile? Attachment { get; init; }
}
public sealed record ReverseExpenseRequest([Required, StringLength(1000, MinimumLength = 3)] string Reason);
