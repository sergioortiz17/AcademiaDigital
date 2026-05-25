using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.User;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/users")]
public class UserController(
    GetProfileUseCase getProfileUseCase,
    UpdateUserUseCase updateUserUseCase) : ApiControllerBase
{
    // GET /api/v1/users/profile  [requiere sesión activa]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var result = await getProfileUseCase.ExecuteAsync(CurrentUserId.Value, ct);

        return Ok(new
        {
            success = true,
            id = result.Id,
            username = result.Username,
            lastName = result.LastName,
            email = result.Email,
            dni = result.Dni,
            gender = result.Gender,
            cuil = result.Cuil,
            birthDate = result.BirthDate,
            phoneCode = result.PhoneCode,
            phone = result.Phone,
            role = result.Role,
            dateJoined = result.DateJoined
        });
    }

    // PUT /api/v1/users/profile  [requiere sesión activa]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var cmd = new UpdateUserCommand(
            TargetUserId: CurrentUserId.Value,
            CurrentUserId: CurrentUserId.Value,
            IsSuperuser: IsSuperuser,
            Username: request.Username,
            LastName: request.LastName,
            Gender: request.Gender,
            Cuil: request.Cuil,
            BirthDate: request.BirthDate,
            PhoneCode: request.PhoneCode,
            Phone: request.Phone);

        var result = await updateUserUseCase.ExecuteAsync(cmd, ct);

        return Ok(new
        {
            success = true,
            id = result.Id,
            username = result.Username,
            lastName = result.LastName,
            email = result.Email,
            dni = result.Dni,
            gender = result.Gender,
            cuil = result.Cuil,
            birthDate = result.BirthDate,
            phoneCode = result.PhoneCode,
            phone = result.Phone,
            date = result.Date
        });
    }

    // POST /api/v1/users/edit  [admin — mantener para compatibilidad]
    [HttpPost("edit")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var cmd = new UpdateUserCommand(
            TargetUserId: request.UserId ?? CurrentUserId.Value,
            CurrentUserId: CurrentUserId.Value,
            IsSuperuser: IsSuperuser,
            Email: request.Email,
            Username: request.Username);

        var result = await updateUserUseCase.ExecuteAsync(cmd, ct);

        return Ok(new { id = result.Id, result.Username, result.Email, date = result.Date });
    }
}

public record UpdateProfileRequest(
    string? Username,
    string? LastName,
    string? Gender,
    string? Cuil,
    DateTime? BirthDate,
    string? PhoneCode,
    string? Phone);

public record UpdateUserRequest(
    long? UserId,
    string? Email,
    string? Username);
