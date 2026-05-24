using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class RegisterUseCase(IUserRepository userRepository)
{
    public async Task<RegisterResult> ExecuteAsync(string email, string username, string password, CancellationToken ct = default)
    {
        var existing = await userRepository.FindByEmailAsync(email, ct);
        if (existing != null)
            throw new EmailAlreadyExistsException();

        var user = await userRepository.CreateAsync(email, username, password, UserRole.Alumno, ct);

        return new RegisterResult(
            Success: true,
            UserId: user.Id,
            Msg: "The user was successfully registered");
    }
}

public record RegisterResult(bool Success, long UserId, string Msg);
