using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

public class ListUsersUseCase(IUserRepository userRepository)
{
    public async Task<IReadOnlyList<UserAdminDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var users = await userRepository.ListAsync(ct);

        return users
            .Select(u => new UserAdminDto(
                u.Id,
                u.Username,
                u.Email,
                u.Role.ToString(),
                u.IsActive,
                u.DateJoined))
            .ToList();
    }
}

public record UserAdminDto(
    long Id,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime DateJoined);
