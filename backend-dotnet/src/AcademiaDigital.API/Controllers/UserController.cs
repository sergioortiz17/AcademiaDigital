using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.User;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/users")]
public class UserController(UpdateUserUseCase updateUserUseCase) : ApiControllerBase
{
    // POST /api/v1/users/edit  [requiere sesión activa]
    [HttpPost("edit")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var result = await updateUserUseCase.ExecuteAsync(
            targetUserId: request.UserId ?? CurrentUserId.Value,
            currentUserId: CurrentUserId.Value,
            isSuperuser: IsSuperuser,
            email: request.Email,
            username: request.Username,
            ct: ct);

        return Ok(new
        {
            id = result.Id,
            result.Username,
            result.Email,
            date = result.Date
        });
    }
}

public record UpdateUserRequest(
    long? UserId,
    string? Email,
    string? Username);
