using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

public class DeleteUserUseCase(IUserRepository userRepository, ISessionRepository sessionRepository)
{
    public async Task ExecuteAsync(long requestingUserId, long targetUserId, CancellationToken ct = default)
    {
        if (requestingUserId == targetUserId)
            throw new ForbiddenException("You cannot delete your own account.");

        await sessionRepository.DeleteByUserAsync(targetUserId, ct);
        await userRepository.DeleteAsync(targetUserId, ct);
    }
}
