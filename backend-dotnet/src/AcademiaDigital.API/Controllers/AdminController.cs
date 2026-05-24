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
    DeleteUserUseCase deleteUserUseCase) : ApiControllerBase
{
    private IActionResult RequireAdmin()
    {
        if (CurrentUserId is null) return Unauthorized(ApiResponse.Fail("Not authenticated."));
        if (CurrentUserRole != UserRole.Admin) return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Admin only."));
        return null!;
    }

    // GET /api/v1/admin/users?search=&page=1&pageSize=20
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var guard = RequireAdmin(); if (guard != null) return guard;
        var result = await getUsersUseCase.ExecuteAsync(search, page, pageSize, ct);
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
