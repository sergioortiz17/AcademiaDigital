using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class LoginUseCase(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ITokenService tokenService,
    IGoogleIdentityVerifier? googleVerifier = null)
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> ExecuteAsync(string email, string password, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim();
        var existingUser = await userRepository.FindByEmailAsync(normalizedEmail, ct);

        if (existingUser?.LockedUntil is DateTime lockedUntil)
        {
            if (lockedUntil > DateTime.UtcNow)
                throw new AccountLockedException(lockedUntil);

            await userRepository.ResetLoginFailuresAsync(existingUser.Id, ct);
        }

        var user = await userRepository.AuthenticateAsync(normalizedEmail, password, ct);
        if (user is null)
        {
            if (existingUser is not null)
            {
                var updated = await userRepository.RecordFailedLoginAsync(existingUser.Id, MaxFailedAttempts, LockoutDuration, ct);
                if (updated.LockedUntil is DateTime newLockedUntil && updated.FailedLoginAttempts >= MaxFailedAttempts)
                    throw new AccountLockedException(newLockedUntil);
            }

            throw new InvalidCredentialsException();
        }

        // La contraseña ya se verificó: avisar que falta el alta no revela nada a un tercero.
        if (user.RegistrationStatus == RegistrationStatus.Pending)
            throw new PendingApprovalException();

        if (!user.IsActive)
            throw new InactiveUserException();

        if (user.FailedLoginAttempts > 0 || user.LockedUntil is not null)
            await userRepository.ResetLoginFailuresAsync(user.Id, ct);

        return await IssueSessionAsync(user, ct);
    }

    /// <summary>
    /// Inicio de sesión con Google: solo para cuentas que ya existen con ese correo (no crea
    /// cuentas). Google confirma que la persona es dueña del correo; las mismas reglas de alta,
    /// cuenta activa y bloqueo que con contraseña.
    /// </summary>
    public async Task<LoginResult> ExecuteWithGoogleAsync(string idToken, CancellationToken ct = default)
    {
        if (googleVerifier is null || string.IsNullOrWhiteSpace(googleVerifier.ClientId))
            throw new InvalidOperationException("El inicio de sesión con Google no está habilitado.");
        var identity = await googleVerifier.VerifyAsync(idToken, ct);
        if (identity is null || !identity.EmailVerified)
            throw new InvalidCredentialsException();

        var user = await userRepository.FindByEmailAsync(identity.Email.Trim(), ct)
            ?? throw new GoogleAccountNotRegisteredException(identity.Email);
        if (user.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
            throw new AccountLockedException(lockedUntil);
        if (user.RegistrationStatus == RegistrationStatus.Pending)
            throw new PendingApprovalException();
        if (!user.IsActive)
            throw new InactiveUserException();
        return await IssueSessionAsync(user, ct);
    }

    private async Task<LoginResult> IssueSessionAsync(Domain.Entities.User user, CancellationToken ct)
    {
        var session = await sessionRepository.FindByUserAsync(user.Id, ct);
        string token;

        if (session != null && !session.IsExpired())
        {
            var sessionRole = tokenService.GetUserRoleFromToken(session.Token);
            if (sessionRole == user.Role)
            {
                token = session.Token;
            }
            else
            {
                await sessionRepository.DeleteAsync(session, ct);
                token = tokenService.GenerateToken(user);
                await sessionRepository.CreateAsync(user.Id, token, ct);
            }
        }
        else
        {
            if (session != null)
                await sessionRepository.DeleteAsync(session, ct);

            token = tokenService.GenerateToken(user);
            await sessionRepository.CreateAsync(user.Id, token, ct);
        }

        return new LoginResult(
            Success: true,
            Token: token,
            User: new UserDto(user.Id, user.Username, user.LastName, user.Dni, user.Email, user.Role));
    }
}

public record LoginResult(bool Success, string Token, UserDto User);
public record UserDto(long Id, string Username, string LastName, string? Dni, string Email, UserRole Role);

/// <summary>El correo de Google no corresponde a ninguna cuenta de la institución.</summary>
public sealed class GoogleAccountNotRegisteredException(string email)
    : Exception($"No hay una cuenta registrada con {email}. Regístrese o pida el alta a Administración.");
