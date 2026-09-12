using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

public class UpdateUserActiveStatusUseCase(IUserRepository userRepository, ISessionRepository sessionRepository)
{
    public async Task<UserSummary> ExecuteAsync(long requestingUserId, long targetUserId, bool isActive, CancellationToken ct = default)
    {
        if (requestingUserId == targetUserId)
            throw new ForbiddenException("No puedes cambiar tu propio estado de actividad.");

        var updated = await userRepository.UpdateActiveStatusAsync(targetUserId, isActive, ct);

        if (!isActive)
            await sessionRepository.DeleteByUserAsync(targetUserId, ct);

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
