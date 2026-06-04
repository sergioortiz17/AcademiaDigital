using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class ResetPasswordUseCase(IUserRepository userRepository, ITokenService tokenService, IPasswordHasher passwordHasher)
{
    public async Task ExecuteAsync(string resetToken, string newPassword, CancellationToken ct = default)
    {
        var userId = tokenService.ValidatePasswordResetToken(resetToken)
            ?? throw new InvalidResetTokenException();

        await userRepository.UpdatePasswordAsync(userId, passwordHasher.Hash(newPassword), ct);
    }
}
