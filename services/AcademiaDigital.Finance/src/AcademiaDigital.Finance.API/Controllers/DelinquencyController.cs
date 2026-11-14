using System.ComponentModel.DataAnnotations;
using AcademiaDigital.Finance.API.Models;
using AcademiaDigital.Finance.Application.Interfaces;
using AcademiaDigital.Finance.Application.UseCases.Delinquency;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.Finance.API.Controllers;

[Route("api/v1/finance/delinquency")]
public sealed class DelinquencyController(
    SyncAcademicDirectoryCommandHandler syncHandler,
    GetDelinquencyDashboardQueryHandler dashboardHandler,
    GetDelinquencyStudentDetailQueryHandler detailHandler,
    GetDelinquencyRulesQueryHandler rulesHandler,
    UpsertDelinquencyRuleCommandHandler upsertRuleHandler,
    IReportDocumentGenerator reportGenerator) : ApiControllerBase
{
    [HttpPost("directory/sync")]
    public async Task<IActionResult> SyncDirectory([FromBody] DirectorySyncRequest request, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        var count = await syncHandler.Handle(new(request.Students.Select(item => new AcademicStudentSnapshotInput(
            item.StudentId, item.StudentCareerId, item.StudentName, item.Dni, item.LegajoNumber,
            item.CareerId, item.CareerName, item.CommissionId, item.CommissionName, item.AcademicYear, item.IsActive)).ToArray()), ct);
        return Ok(new { success = true, synchronized = count });
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int? careerId, [FromQuery] int? commissionId, [FromQuery] int? academicYear,
        [FromQuery] long? studentId, [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        return Ok(await dashboardHandler.Handle(new(careerId, commissionId, academicYear, studentId, asOf), ct));
    }

    [HttpGet("students/{studentId:long}")]
    public async Task<IActionResult> GetStudent(long studentId, [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        return Ok(await detailHandler.Handle(studentId, asOf, ct));
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetRules(CancellationToken ct)
    {
        var guard = RequireTreasury();
        return guard ?? Ok(await rulesHandler.Handle(ct));
    }

    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] DelinquencyRuleRequest request, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        var result = await upsertRuleHandler.Handle(new(null, request.Name, request.MinimumOutstandingAmount,
            request.MinimumDaysOverdue, request.IsActive, CurrentUserId!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("rules/{id:long}")]
    public async Task<IActionResult> UpdateRule(long id, [FromBody] DelinquencyRuleRequest request, CancellationToken ct)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        return Ok(await upsertRuleHandler.Handle(new(id, request.Name, request.MinimumOutstandingAmount,
            request.MinimumDaysOverdue, request.IsActive, CurrentUserId!.Value), ct));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "excel", [FromQuery] int? careerId = null,
        [FromQuery] int? commissionId = null, [FromQuery] int? academicYear = null,
        [FromQuery] DateOnly? asOf = null, CancellationToken ct = default)
    {
        var guard = RequireTreasury();
        if (guard is not null) return guard;
        var dashboard = await dashboardHandler.Handle(new(careerId, commissionId, academicYear, null, asOf), ct);
        string[] headers = ["Alumno", "DNI", "Legajo", "Carrera", "Comisión", "Ciclo", "Saldo", "Vencido", "Días", "Alertas"];
        var rows = dashboard.Students.Select(item => (IReadOnlyList<string>)new[]
        {
            item.StudentName, item.Dni, item.LegajoNumber, item.CareerName, item.CommissionName ?? "-",
            item.AcademicYear.ToString(), item.OutstandingAmount.ToString("0.00"), item.OverdueAmount.ToString("0.00"),
            item.MaximumDaysOverdue.ToString(), string.Join(", ", item.TriggeredAlerts)
        }).ToArray();
        var report = format.Equals("pdf", StringComparison.OrdinalIgnoreCase)
            ? reportGenerator.GeneratePdf("Reporte de morosidad", headers, rows, $"morosidad-{dashboard.AsOf:yyyyMMdd}.pdf")
            : reportGenerator.GenerateExcel("Reporte de morosidad", headers, rows, $"morosidad-{dashboard.AsOf:yyyyMMdd}.xls");
        return File(report.Content, report.ContentType, report.FileName);
    }

    private IActionResult? RequireTreasury()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        return CurrentUserRole is UserRole.Admin or UserRole.Tesoreria
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Treasury access required."));
    }
}

public sealed record DirectoryStudentRequest(
    [Range(1, long.MaxValue)] long StudentId,
    [Range(1, long.MaxValue)] long StudentCareerId,
    [Required, StringLength(200)] string StudentName,
    [Required, StringLength(20)] string Dni,
    [Required, StringLength(50)] string LegajoNumber,
    [Range(1, int.MaxValue)] int CareerId,
    [Required, StringLength(200)] string CareerName,
    [Range(1, int.MaxValue)] int? CommissionId,
    [StringLength(100)] string? CommissionName,
    [Range(2000, 2200)] int AcademicYear,
    bool IsActive);

public sealed record DirectorySyncRequest([Required, MinLength(1), MaxLength(5000)] IReadOnlyList<DirectoryStudentRequest> Students);
public sealed record DelinquencyRuleRequest(
    [Required, StringLength(150)] string Name,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal MinimumOutstandingAmount,
    [Range(0, 36500)] int MinimumDaysOverdue,
    bool IsActive = true);
