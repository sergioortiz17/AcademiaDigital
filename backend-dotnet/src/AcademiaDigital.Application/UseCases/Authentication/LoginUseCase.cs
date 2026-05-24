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
    public async Task<LoginResult> ExecuteAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userRepository.AuthenticateAsync(email, password, ct)
            ?? throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new InactiveUserException();

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
