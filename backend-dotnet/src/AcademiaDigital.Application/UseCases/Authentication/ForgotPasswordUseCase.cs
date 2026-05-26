using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class ForgotPasswordUseCase(IUserRepository userRepository, ITokenService tokenService)
{
    public async Task<ForgotPasswordResult> ExecuteAsync(string email, CancellationToken ct = default)
    {
        var user = await userRepository.FindByEmailAsync(email.Trim(), ct);

        // No revelar si el email existe o no (seguridad)
        if (user is null)
            return new ForgotPasswordResult(true, null);

        var resetToken = tokenService.GeneratePasswordResetToken(user.Id);
        return new ForgotPasswordResult(true, resetToken);
    }
}

public record ForgotPasswordResult(bool Success, string? ResetToken);
