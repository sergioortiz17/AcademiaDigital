using System.Text.RegularExpressions;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

public class GetUserByDniUseCase(IUserRepository userRepository, IStudentRepository studentRepository)
{
    private static readonly Regex DniRegex = new("^\\d{7,8}$", RegexOptions.Compiled);

    public async Task<GetUserByDniResult> ExecuteAsync(string dni, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dni))
            throw new ArgumentException("DNI is required.");

        var normalizedDni = dni.Trim();
        if (!DniRegex.IsMatch(normalizedDni))
            throw new ArgumentException("DNI must contain only numbers and have 7 or 8 digits.");

        var user = await userRepository.FindByDniAsync(normalizedDni, ct);
        if (user is null)
            throw new KeyNotFoundException($"Student with DNI {normalizedDni} not found.");

        var student = await studentRepository.FindByUserIdAsync(user.Id, ct);
        if (student is null)
            throw new KeyNotFoundException($"Student with DNI {normalizedDni} not found.");

        return new GetUserByDniResult(
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
            Role: user.Role,
            IsActive: user.IsActive,
            DateJoined: user.DateJoined,
            FailedLoginAttempts: user.FailedLoginAttempts,
            LockedUntil: user.LockedUntil);
    }
}

public record GetUserByDniResult(
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
    UserRole Role,
    bool IsActive,
    DateTime DateJoined,
    int FailedLoginAttempts,
    DateTime? LockedUntil);
