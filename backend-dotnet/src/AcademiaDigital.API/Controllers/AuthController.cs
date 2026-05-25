using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/users")]
public class AuthController(
    LoginUseCase loginUseCase,
    RegisterUseCase registerUseCase,
    LogoutUseCase logoutUseCase) : ApiControllerBase
{
    // POST /api/v1/users/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await loginUseCase.ExecuteAsync(request.Email, request.Password, ct);
        return Ok(new
        {
            success = result.Success,
            token = result.Token,
            user = new { _id = result.User.Id, result.User.Username, result.User.Email, role = (int)result.User.Role }
        });
    }

    // POST /api/v1/users/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await registerUseCase.ExecuteAsync(request.Email, request.Name, request.LastName, request.Password, request.Dni, ct);
        return StatusCode(StatusCodes.Status201Created, new
        {
            success = result.Success,
            userID = result.UserId,
            msg = result.Msg
        });
    }

    // POST /api/v1/users/logout  [requiere sesión activa]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        var result = await logoutUseCase.ExecuteAsync(CurrentUserId.Value, ct);
        return Ok(new { success = result.Success, msg = result.Msg });
    }

    // POST /api/v1/users/checkSession  [requiere sesión activa]
    [HttpPost("checkSession")]
    public IActionResult CheckSession()
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        return Ok(new { success = true });
    }
}

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password);

public record RegisterRequest(
    [property: JsonPropertyName("name")]
    [Required] string Name,

    [property: JsonPropertyName("lastname")]
    [Required] string LastName,

    [property: JsonPropertyName("email")]
    [Required][EmailAddress] string Email,

    [property: JsonPropertyName("password")]
    [Required][MinLength(4)] string Password,

    [property: JsonPropertyName("DNI")]
    [Required]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "DNI must contain only numbers and have 7 or 8 digits.")]
    string Dni);
