using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

public class ChangeUserStatusUseCase(
    IUserRepository userRepository,
    IAdminAuditLogRepository auditLogRepository)
{
    public async Task<UserAdminDto> ExecuteAsync(
        long targetUserId,
        long currentUserId,
        bool isActive,
        CancellationToken ct = default)
    {
        if (targetUserId == currentUserId && !isActive)
            throw new UnauthorizedUserUpdateException("Users cannot deactivate their own account");

        var user = await userRepository.FindByIdAsync(targetUserId, ct)
            ?? throw new UserNotFoundException();

        var previousStatus = user.IsActive;
        user.IsActive = isActive;
        var updated = await userRepository.UpdateAsync(user, ct);

        await auditLogRepository.AddAsync(new AdminAuditLog
        {
            ActorUserId = currentUserId,
            TargetUserId = updated.Id,
            Action = "UserStatusChanged",
            Detail = $"Changed status for user '{updated.Email}' from '{previousStatus}' to '{updated.IsActive}'.",
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new UserAdminDto(
            updated.Id,
            updated.Username,
            updated.Dni,
            updated.Email,
            updated.Role.ToString(),
            updated.IsActive,
            updated.DateJoined);
    }
}
