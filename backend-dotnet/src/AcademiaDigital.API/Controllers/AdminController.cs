using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Admin;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/admin")]
public class AdminController(
    GetUsersUseCase getUsersUseCase,
    GetUserByDniUseCase getUserByDniUseCase,
    UpdateUserRoleUseCase updateUserRoleUseCase,
    UpdateUserActiveStatusUseCase updateUserActiveStatusUseCase,
    DeleteUserUseCase deleteUserUseCase) : ApiControllerBase
{
    private IActionResult RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Admin) return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
        return null!;
    }

    private IActionResult RequireAdminProfessorOrCoordinator()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole is not UserRole.Admin and not UserRole.Profesor and not UserRole.Coordinador)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin, Profesor or Coordinador only."));
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

    // GET /api/v1/admin/users/search-by-dni?dni=12345678
    [HttpGet("users/search-by-dni")]
    public async Task<IActionResult> GetUserByDni([FromQuery] string dni, CancellationToken ct = default)
    {
        var guard = RequireAdminProfessorOrCoordinator(); if (guard != null) return guard;
        var result = await getUserByDniUseCase.ExecuteAsync(dni, ct);
        return Ok(new { success = true, user = result });
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

    // DELETE /api/v1/admin/users/{id}
    [HttpDelete("users/{id:long}")]
    public async Task<IActionResult> DeleteUser(long id, CancellationToken ct)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        await deleteUserUseCase.ExecuteAsync(CurrentUserId!.Value, id, ct);
        return Ok(new { success = true, msg = "User deleted." });
    }
}

public record UpdateRoleRequest([Required] UserRole Role);
public record UpdateActiveStatusRequest([Required] bool IsActive);
