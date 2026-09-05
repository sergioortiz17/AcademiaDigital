using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Finance.API.Models;
using AcademiaDigital.Finance.Application.UseCases.Finance;
using AcademiaDigital.Finance.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.Finance.API.Controllers;

[Route("api/v1/finance")]
public sealed class FinanceController(
    GetFinancialConceptsQueryHandler conceptsQuery,
    CreateFinancialConceptCommandHandler createConceptHandler,
    UpdateFinancialConceptCommandHandler updateConceptHandler,
    GetFinancialRatesQueryHandler ratesQuery,
    UpsertFinancialRateCommandHandler upsertRateHandler,
    GetFinancialBenefitsQueryHandler benefitsQuery,
    CreateFinancialBenefitCommandHandler createBenefitHandler,
    GetBillingPlansQueryHandler plansQuery,
    CreateBillingPlanCommandHandler createPlanHandler,
    GenerateStudentDebtsCommandHandler generateDebtsHandler,
    GetStudentDebtsQueryHandler debtsQuery,
    GetStudentDebtSummaryQueryHandler summaryQuery) : ApiControllerBase
{
    [HttpGet("concepts")]
    public async Task<IActionResult> GetConcepts(CancellationToken ct)
    {
        var guard = RequireAdmin();
        return guard ?? Ok(await conceptsQuery.Handle(ct));
    }

    [HttpPost("concepts")]
    public async Task<IActionResult> CreateConcept([FromBody] FinancialConceptRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await createConceptHandler.Handle(new(request.Code, request.Name, request.Description), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("concepts/{id:int}")]
    public async Task<IActionResult> UpdateConcept(int id, [FromBody] UpdateFinancialConceptRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await updateConceptHandler.Handle(new(id, request.Code, request.Name, request.Description, request.IsActive), ct));
    }

    [HttpGet("rates")]
    public async Task<IActionResult> GetRates([FromQuery] int? careerId, [FromQuery] int? academicYear, CancellationToken ct)
    {
        var guard = RequireAdmin();
        return guard ?? Ok(await ratesQuery.Handle(careerId, academicYear, ct));
    }

    [HttpPost("rates")]
    public async Task<IActionResult> CreateRate([FromBody] FinancialRateRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await upsertRateHandler.Handle(ToCommand(null, request), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("rates/{id:long}")]
    public async Task<IActionResult> UpdateRate(long id, [FromBody] FinancialRateRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await upsertRateHandler.Handle(ToCommand(id, request), ct));
    }

    [HttpGet("benefits")]
    public async Task<IActionResult> GetBenefits(CancellationToken ct)
    {
        var guard = RequireAdmin();
        return guard ?? Ok(await benefitsQuery.Handle(ct));
    }

    [HttpPost("benefits")]
    public async Task<IActionResult> CreateBenefit([FromBody] FinancialBenefitRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await createBenefitHandler.Handle(new(
            request.Code, request.Name, request.Kind, request.ScholarshipId, request.CareerId,
            request.StudentCondition, request.Percentage, request.ValidFrom, request.ValidTo), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans([FromQuery] int? careerId, [FromQuery] int? academicYear, CancellationToken ct)
    {
        var guard = RequireAdmin();
        return guard ?? Ok(await plansQuery.Handle(careerId, academicYear, ct));
    }

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] BillingPlanRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var created = await createPlanHandler.Handle(new(
            request.Name, request.CareerId, request.AcademicYear,
            request.Items.Select(item => new CreateBillingPlanItemCommand(item.ConceptId, item.InstallmentNumber, item.DueDate)).ToArray(),
            CurrentUserId ?? 0), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    // ── Contract endpoint (called by the monolito, fire-and-forget) ─────────────
    // POST /api/v1/finance/debts/generate
    //   body: { studentId, careerId, studentCareerId, billingPlanId, academicYear, condition?, grantedScholarshipIds? }
    [HttpPost("debts/generate")]
    public async Task<IActionResult> GenerateDebts(
        [FromBody] GenerateDebtsRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var result = await generateDebtsHandler.Handle(new(
            request.BillingPlanId,
            request.StudentId,
            request.CareerId,
            request.StudentCareerId,
            request.AcademicYear,
            request.Condition ?? StudentStatus.Regular,
            request.GrantedScholarshipIds ?? Array.Empty<int>(),
            idempotencyKey,
            CurrentUserId ?? 0), ct);
        return Ok(new
        {
            batchId = result.BatchPublicId,
            generatedDebtCount = result.GeneratedDebtCount,
            totalAmount = result.GeneratedTotal,
            result.Debts
        });
    }

    // GET /api/v1/finance/students/{studentId}/debts
    [HttpGet("students/{studentId:long}/debts")]
    public async Task<IActionResult> GetStudentDebts(long studentId, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        var debts = await debtsQuery.Handle(studentId, ct);
        return Ok(new { studentId, debts });
    }

    // GET /api/v1/finance/students/{studentId}/debts/summary
    [HttpGet("students/{studentId:long}/debts/summary")]
    public async Task<IActionResult> GetStudentDebtSummary(long studentId, CancellationToken ct)
    {
        var guard = RequireAdmin();
        if (guard is not null) return guard;
        return Ok(await summaryQuery.Handle(studentId, ct));
    }

    [HttpGet("debts/me")]
    public async Task<IActionResult> GetMyDebts([FromQuery, Range(1, long.MaxValue)] long studentId, CancellationToken ct)
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Alumno)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Only students can query their own debts."));
        return Ok(await debtsQuery.Handle(studentId, ct));
    }

    private IActionResult? RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole == UserRole.Admin
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
    }

    private static UpsertFinancialRateCommand ToCommand(long? id, FinancialRateRequest request)
        => new(id, request.ConceptId, request.CareerId, request.AcademicYear, request.StudentCondition,
            request.Amount, request.SurchargePercentage, request.IsActive);
}

public sealed record GenerateDebtsRequest(
    [Range(1, long.MaxValue)] long StudentId,
    [Range(1, int.MaxValue)] int CareerId,
    [Range(1, long.MaxValue)] long StudentCareerId,
    [Range(1, long.MaxValue)] long BillingPlanId,
    [Range(2000, 2200)] int AcademicYear,
    StudentStatus? Condition,
    IReadOnlyCollection<int>? GrantedScholarshipIds);

public sealed record FinancialConceptRequest(
    [Required, StringLength(30, MinimumLength = 2)] string Code,
    [Required, StringLength(150, MinimumLength = 2)] string Name,
    [StringLength(500)] string? Description);

public sealed record UpdateFinancialConceptRequest(
    [Required, StringLength(30, MinimumLength = 2)] string Code,
    [Required, StringLength(150, MinimumLength = 2)] string Name,
    [StringLength(500)] string? Description,
    bool IsActive = true);

public sealed record FinancialRateRequest(
    [Range(1, int.MaxValue)] int ConceptId,
    [Range(1, int.MaxValue)] int CareerId,
    [Range(2000, 2200)] int AcademicYear,
    StudentStatus? StudentCondition,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Amount,
    [Range(typeof(decimal), "0", "100")] decimal SurchargePercentage,
    bool IsActive = true);

public sealed record FinancialBenefitRequest(
    [Required, StringLength(30, MinimumLength = 2)] string Code,
    [Required, StringLength(150, MinimumLength = 2)] string Name,
    FinancialBenefitKind Kind,
    [Range(1, int.MaxValue)] int? ScholarshipId,
    [Range(1, int.MaxValue)] int? CareerId,
    StudentStatus? StudentCondition,
    [Range(typeof(decimal), "0.01", "100")] decimal Percentage,
    DateOnly? ValidFrom,
    DateOnly? ValidTo);

public sealed record BillingPlanItemRequest(
    [Range(1, int.MaxValue)] int ConceptId,
    [Range(1, int.MaxValue)] int InstallmentNumber,
    DateOnly DueDate);

public sealed record BillingPlanRequest(
    [Required, StringLength(150, MinimumLength = 2)] string Name,
    [Range(1, int.MaxValue)] int CareerId,
    [Range(2000, 2200)] int AcademicYear,
    [Required, MinLength(1)] IReadOnlyList<BillingPlanItemRequest> Items);
