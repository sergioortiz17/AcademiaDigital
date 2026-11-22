using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Admin;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/admin")]
public class AdminController(
    GetUsersUseCase getUsersUseCase,
    UpdateUserRoleUseCase updateUserRoleUseCase,
    UpdateUserActiveStatusUseCase updateUserActiveStatusUseCase,
    DeleteUserUseCase deleteUserUseCase,
    GetApplicantsUseCase getApplicantsUseCase,
    ApproveApplicantsUseCase approveApplicantsUseCase,
    ImportApplicantsUseCase importApplicantsUseCase) : ApiControllerBase
{
    // POST /api/v1/admin/applicants/import — filas leídas del Excel de prematriculados
    [HttpPost("applicants/import")]
    public async Task<IActionResult> ImportApplicants([FromBody] ImportApplicantsRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        var result = await importApplicantsUseCase.ExecuteAsync(request.Rows.Select(r => new ApplicantImportRow(
            r.RowNumber, r.Dni, r.LastName, r.FirstName, r.Email, r.CareerCode)).ToArray(), ct);
        return Ok(new { success = true, result.Created, result.Failed });
    }

    private IActionResult RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Admin) return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
        return null!;
    }

    // GET /api/v1/admin/users?search=&role=&page=1&pageSize=20
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        var result = await getUsersUseCase.ExecuteAsync(search, role, page, pageSize, ct);
        return Ok(new { success = true, result.Users, result.Total, result.Page, result.PageSize });
    }

    // PATCH /api/v1/admin/users/{id}/role
    [HttpPatch("users/{id:long}/role")]
    public async Task<IActionResult> UpdateRole(long id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        var updated = await updateUserRoleUseCase.ExecuteAsync(CurrentUserId!.Value, id, request.Role, ct);
        return Ok(new { success = true, user = updated });
    }

    // PATCH /api/v1/admin/users/{id}/active
    [HttpPatch("users/{id:long}/active")]
    public async Task<IActionResult> UpdateActiveStatus(long id, [FromBody] UpdateActiveStatusRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        var updated = await updateUserActiveStatusUseCase.ExecuteAsync(CurrentUserId!.Value, id, request.IsActive, ct);
        return Ok(new { success = true, user = updated });
    }

    // GET /api/v1/admin/applicants?status=pending|approved&role=&search=&page=1&pageSize=20
    // Aspirantes del registro público: pendientes de alta o dados de alta en los últimos 7 días.
    [HttpGet("applicants")]
    public async Task<IActionResult> GetApplicants(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        var approved = string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase);
        var result = await getApplicantsUseCase.ExecuteAsync(search, role, approved, page, pageSize, ct);
        return Ok(new { success = true, result.Items, result.Total, result.Page, result.PageSize });
    }

    // POST /api/v1/admin/applicants/approve  { "userIds": [1, 2, 3] }
    [HttpPost("applicants/approve")]
    public async Task<IActionResult> ApproveApplicants([FromBody] ApproveApplicantsRequest request, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        var result = await approveApplicantsUseCase.ExecuteAsync(CurrentUserId!.Value, request.UserIds, ct);
        return Ok(new { success = true, result.ApprovedIds, result.Skipped });
    }

    // DELETE /api/v1/admin/users/{id}
    [HttpDelete("users/{id:long}")]
    public async Task<IActionResult> DeleteUser(long id, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        await deleteUserUseCase.ExecuteAsync(CurrentUserId!.Value, id, ct);
        return Ok(new { success = true, msg = "User deleted." });
    }
}

public record ApproveApplicantsRequest([Required] long[] UserIds);
public record UpdateRoleRequest([Required] UserRole Role);
public record UpdateActiveStatusRequest([Required] bool IsActive);

public record ImportApplicantsRequest([System.ComponentModel.DataAnnotations.Required] IReadOnlyList<ImportApplicantRowRequest> Rows);
public record ImportApplicantRowRequest(int RowNumber, string? Dni, string? LastName, string? FirstName, string? Email, string? CareerCode);
