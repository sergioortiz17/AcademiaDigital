using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class RegisterUseCase(IUserRepository userRepository)
{
    public async Task<RegisterResult> ExecuteAsync(string email, string name, string lastName, string password, string dni, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim();
        var normalizedDni = dni.Trim();

        var existingEmail = await userRepository.FindByEmailAsync(normalizedEmail, ct);
        if (existingEmail != null)
            throw new EmailAlreadyExistsException();

        var existingDni = await userRepository.FindByDniAsync(normalizedDni, ct);
        if (existingDni != null)
            throw new DniAlreadyExistsException();

        var user = await userRepository.CreateAsync(normalizedEmail, name, lastName, password, normalizedDni, UserRole.Alumno, ct);

        return new RegisterResult(
            Success: true,
            UserId: user.Id,
            Msg: "The user was successfully registered");
    }
}

public record RegisterResult(bool Success, long UserId, string Msg);
