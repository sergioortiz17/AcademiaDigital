using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.Authentication;

/// <summary>
/// Cambio de contraseña con el token recibido por correo. El token tiene que ser de esta
/// institución y de la contraseña actual (un solo uso). Al cambiarla se cierran las sesiones
/// abiertas del usuario.
/// </summary>
public class ResetPasswordUseCase(
    IUserRepository userRepository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    ISessionRepository sessionRepository,
    ITenantContext tenantContext)
{
    public async Task ExecuteAsync(string resetToken, string newPassword, CancellationToken ct = default)
    {
        var claims = tokenService.ReadPasswordResetToken(resetToken)
            ?? throw new InvalidResetTokenException();

        if (!string.Equals(claims.Tenant, tenantContext.Current.Slug, StringComparison.Ordinal))
            throw new InvalidResetTokenException();

        var user = await userRepository.FindByIdAsync(claims.UserId, ct);
        if (user is null || !user.IsActive
            || !string.Equals(claims.PasswordStamp, PasswordResetStamp.For(user.Password), StringComparison.Ordinal))
            throw new InvalidResetTokenException();

        await userRepository.UpdatePasswordAsync(user.Id, passwordHasher.Hash(newPassword), ct);
        await sessionRepository.DeleteByUserAsync(user.Id, ct);
    }
}
