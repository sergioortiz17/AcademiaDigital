using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class LoginUseCase(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ITokenService tokenService)
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

        if (!user.IsActive)
            throw new InactiveUserException();

        if (user.FailedLoginAttempts > 0 || user.LockedUntil is not null)
            await userRepository.ResetLoginFailuresAsync(user.Id, ct);

        var session = await sessionRepository.FindByUserAsync(user.Id, ct);
        string token;

        if (session != null && !session.IsExpired())
        {
            token = session.Token;
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
            User: new UserDto(user.Id, user.Username, user.Email, user.Role));
    }
}

public record LoginResult(bool Success, string Token, UserDto User);
public record UserDto(long Id, string Username, string Email, UserRole Role);
