using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

public class GetUsersUseCase(IUserRepository userRepository)
{
    public async Task<GetUsersResult> ExecuteAsync(string? search, UserRole? role, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var skip = (page - 1) * pageSize;

        var (users, total) = await userRepository.GetAllAsync(search, role, skip, pageSize, ct);

        var summaries = users.Select(u => new UserSummary(
            Id: u.Id,
            Username: u.Username,
            Email: u.Email,
            Dni: u.Dni,
            Role: u.Role,
            IsActive: u.IsActive,
            DateJoined: u.DateJoined
        )).ToList();

        return new GetUsersResult(summaries, total, page, pageSize);
    }
}

public record UserSummary(long Id, string Username, string Email, string? Dni, UserRole Role, bool IsActive, DateTime DateJoined);
public record GetUsersResult(List<UserSummary> Users, int Total, int Page, int PageSize);
