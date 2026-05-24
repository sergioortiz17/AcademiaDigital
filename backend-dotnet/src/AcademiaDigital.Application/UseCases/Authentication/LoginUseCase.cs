using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class LoginUseCase(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher)
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> ExecuteAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userRepository.FindByEmailForLoginAsync(email, ct)
            ?? throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new InactiveUserException();

        var now = DateTime.UtcNow;

        if (user.LockedUntil is not null && user.LockedUntil > now)
            throw new AccountLockedException();

        if (!passwordHasher.Verify(password, user.Password))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                user.LockedUntil = now.Add(LockoutDuration);

            await userRepository.UpdateAsync(user, ct);

            if (user.LockedUntil is not null && user.LockedUntil > now)
                throw new AccountLockedException();

            throw new InvalidCredentialsException();
        }

        if (user.FailedLoginAttempts > 0 || user.LockedUntil is not null)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await userRepository.UpdateAsync(user, ct);
        }

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
            User: new UserDto(user.Id, user.Username, user.Email, user.Role.ToString()));
    }
}

public record LoginResult(bool Success, string Token, UserDto User);
public record UserDto(long Id, string Username, string Email, string Role);
