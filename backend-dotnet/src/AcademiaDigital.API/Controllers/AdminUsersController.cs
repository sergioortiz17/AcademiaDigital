using System.ComponentModel.DataAnnotations;
using AcademiaDigital.API.Models;
using AcademiaDigital.API.Security;
using AcademiaDigital.Application.UseCases.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Authorize(Policy = AppPolicies.CanManageUsers)]
[Route("api/v1/admin/users")]
public class AdminUsersController(
    ListUsersUseCase listUsersUseCase,
    CreateInternalUserUseCase createInternalUserUseCase,
    ChangeUserRoleUseCase changeUserRoleUseCase,
    ChangeUserStatusUseCase changeUserStatusUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var users = await listUsersUseCase.ExecuteAsync(ct);
        return Ok(ApiResponse.Ok(users));
    }

    [HttpPost]
    public async Task<IActionResult> CreateInternalUser([FromBody] CreateInternalUserRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var user = await createInternalUserUseCase.ExecuteAsync(
            CurrentUserId.Value,
            request.Email,
            request.Username,
            request.Password,
            request.Dni,
            request.Role,
            ct);

        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(user));
    }

    [HttpPatch("{id:long}/role")]
    public async Task<IActionResult> ChangeRole(long id, [FromBody] ChangeUserRoleRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var user = await changeUserRoleUseCase.ExecuteAsync(id, CurrentUserId.Value, request.Role, ct);
        return Ok(ApiResponse.Ok(user));
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeUserStatusRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var user = await changeUserStatusUseCase.ExecuteAsync(id, CurrentUserId.Value, request.IsActive, ct);
        return Ok(ApiResponse.Ok(user));
    }
}

public record CreateInternalUserRequest(
    [Required][EmailAddress] string Email,
    [Required] string Username,
    [Required][MinLength(4)] string Password,
    [Required][RegularExpression(@"^\d{7,8}$")] string Dni,
    [Required] string Role);

public record ChangeUserRoleRequest([Required] string Role);

public record ChangeUserStatusRequest(bool IsActive);
