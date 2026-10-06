using AcademiaDigital.API.Models;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Application.UseCases.User;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/users")]
public class AuthController(
    LoginUseCase loginUseCase,
    RegisterUseCase registerUseCase,
    LogoutUseCase logoutUseCase,
    ForgotPasswordUseCase forgotPasswordUseCase,
    ResetPasswordUseCase resetPasswordUseCase,
    ChangePasswordUseCase changePasswordUseCase) : ApiControllerBase
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
            user = new { _id = result.User.Id, result.User.Username, result.User.LastName, result.User.Dni, result.User.Email, role = (int)result.User.Role }
        });
    }

    // POST /api/v1/users/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await registerUseCase.ExecuteAsync(request.Email, request.Name, request.LastName, request.Password, request.Dni, request.CareerId, ct);
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

    // PUT /api/v1/users/change-password  [requiere sesión activa]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (CurrentUserId is null)
            return Unauthorized(ApiResponse.Fail("User is not logged on."));

        await changePasswordUseCase.ExecuteAsync(CurrentUserId.Value, request.CurrentPassword, request.NewPassword, ct);
        return Ok(new { success = true, msg = "Contraseña actualizada correctamente" });
    }

    // POST /api/v1/users/forgot-password  [público]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await forgotPasswordUseCase.ExecuteAsync(request.Email, ct);
        return Ok(new { success = result.Success, resetToken = result.ResetToken, msg = "Si el correo está registrado, recibirás instrucciones" });
    }

    // POST /api/v1/users/reset-password  [público]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await resetPasswordUseCase.ExecuteAsync(request.ResetToken, request.NewPassword, ct);
        return Ok(new { success = true, msg = "Contraseña restablecida correctamente" });
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
    string Dni,

    [property: JsonPropertyName("careerId")]
    [Required][Range(1, int.MaxValue)] int CareerId);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][MinLength(8)] string NewPassword);

public record ForgotPasswordRequest(
    [Required][EmailAddress] string Email);

public record ResetPasswordRequest(
    [Required] string ResetToken,
    [Required][MinLength(8)] string NewPassword);
