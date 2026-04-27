using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

public class UpdateUserUseCase(IUserRepository userRepository)
{
    public async Task<UpdateUserResult> ExecuteAsync(
        long targetUserId,
        long currentUserId,
        bool isSuperuser,
        string? email,
        string? username,
        CancellationToken ct = default)
    {
        if (currentUserId != targetUserId && !isSuperuser)
            throw new UnauthorizedUserUpdateException();

        var user = await userRepository.FindByIdAsync(targetUserId, ct)
            ?? throw new UnauthorizedUserUpdateException();

        if (email is not null) user.Email = email;
        if (username is not null) user.Username = username;

        var updated = await userRepository.UpdateAsync(user, ct);

        return new UpdateUserResult(
            Id: updated.Id,
            Username: updated.Username,
            Email: updated.Email,
            Date: updated.DateJoined);
    }
}

public record UpdateUserResult(long Id, string Username, string Email, DateTime Date);
