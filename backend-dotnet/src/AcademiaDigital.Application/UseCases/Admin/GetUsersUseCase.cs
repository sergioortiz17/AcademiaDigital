using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

public class GetUsersUseCase(IUserRepository userRepository)
{
    public async Task<List<UserSummary>> ExecuteAsync(CancellationToken ct = default)
    {
        var users = await userRepository.GetAllAsync(ct);
        return users.Select(u => new UserSummary(
            Id: u.Id,
            Username: u.Username,
            Email: u.Email,
            Role: u.Role,
            IsActive: u.IsActive,
            DateJoined: u.DateJoined
        )).ToList();
    }
}

public record UserSummary(long Id, string Username, string Email, UserRole Role, bool IsActive, DateTime DateJoined);
