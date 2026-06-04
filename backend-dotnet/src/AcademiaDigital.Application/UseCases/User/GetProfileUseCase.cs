using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

public class GetProfileUseCase(IUserRepository userRepository)
{
    public async Task<ProfileResult> ExecuteAsync(long userId, CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        return new ProfileResult(
            Id: user.Id,
            Username: user.Username,
            LastName: user.LastName,
            Email: user.Email,
            Dni: user.Dni,
            Gender: user.Gender,
            Cuil: user.Cuil,
            BirthDate: user.BirthDate,
            PhoneCode: user.PhoneCode,
            Phone: user.Phone,
            Role: (int)user.Role,
            DateJoined: user.DateJoined);
    }
}

public record ProfileResult(
    long Id,
    string Username,
    string LastName,
    string Email,
    string? Dni,
    string? Gender,
    string? Cuil,
    DateTime? BirthDate,
    string? PhoneCode,
    string? Phone,
    int Role,
    DateTime DateJoined);
