using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

public class UpdateUserRoleUseCase(IUserRepository userRepository)
{
    public async Task<UserSummary> ExecuteAsync(long requestingUserId, long targetUserId, UserRole newRole, CancellationToken ct = default)
    {
        if (requestingUserId == targetUserId)
            throw new ForbiddenException("No puedes cambiar tu propio rol.");

        var updated = await userRepository.UpdateRoleAsync(targetUserId, newRole, ct);

        return new UserSummary(
            Id: updated.Id,
            Username: updated.Username,
            Email: updated.Email,
            Dni: updated.Dni,
            Role: updated.Role,
            IsActive: updated.IsActive,
            DateJoined: updated.DateJoined);
    }
}
