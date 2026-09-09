using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Finance.API.Models;
using AcademiaDigital.Finance.Application.UseCases.Payments;
using AcademiaDigital.Finance.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.Finance.API.Controllers;

[Route("api/v1/finance/payment-methods")]
public sealed class PaymentMethodsController(GetPaymentMethodsQueryHandler handler) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return Ok(await handler.Handle(ct));
    }
}

[Route("api/v1/payments")]
public sealed class PaymentsController(
    CreatePaymentCommandHandler createHandler,
    ConfirmPaymentCommandHandler confirmHandler,
    ReconcilePaymentCommandHandler reconcileHandler,
    ReversePaymentCommandHandler reverseHandler,
    GetPaymentsQueryHandler queryHandler) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await createHandler.Handle(new(
            request.StudentId,
            request.StudentName,
            request.StudentDni,
            request.PaymentMethodId,
            request.Amount,
            request.ExternalReference,
            request.Notes,
            request.Allocations.Select(item => new CreatePaymentAllocationCommand(item.DebtPublicId, item.Amount)).ToArray(),
            CurrentUserId ?? 0), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("{publicId:guid}/confirm")]
    public async Task<IActionResult> Confirm(
        Guid publicId,
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(100, MinimumLength = 8)] string idempotencyKey,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await confirmHandler.Handle(new(publicId, idempotencyKey, CurrentUserId ?? 0), ct));
    }

    [HttpPost("{publicId:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(Guid publicId, [FromBody] ReconcilePaymentRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reconcileHandler.Handle(new(publicId, request.Decision, request.Note, CurrentUserId ?? 0), ct));
    }

    [HttpPost("{publicId:guid}/reverse")]
    public async Task<IActionResult> Reverse(Guid publicId, [FromBody] ReversePaymentRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await reverseHandler.Handle(new(publicId, request.Reason, CurrentUserId ?? 0), ct));
    }

    [HttpGet]
    public async Task<IActionResult> GetByStudent([FromQuery, Range(1, long.MaxValue)] long studentId, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await queryHandler.Handle(studentId, ct));
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
    }
}

public sealed record CreatePaymentAllocationRequest(
    Guid DebtPublicId,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Amount);

public sealed record CreatePaymentRequest(
    [Range(1, long.MaxValue)] long StudentId,
    [Required, StringLength(200, MinimumLength = 1)] string StudentName,
    [Required, StringLength(20, MinimumLength = 7)] string StudentDni,
    [Range(1, int.MaxValue)] int PaymentMethodId,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Amount,
    [StringLength(100)] string? ExternalReference,
    [StringLength(500)] string? Notes,
    [Required, MinLength(1)] IReadOnlyList<CreatePaymentAllocationRequest> Allocations);

public sealed record ReconcilePaymentRequest(
    PaymentReconciliationDecision Decision,
    [StringLength(500)] string? Note);

public sealed record ReversePaymentRequest(
    [Required, StringLength(500, MinimumLength = 5)] string Reason);
