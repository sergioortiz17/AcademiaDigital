using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

public class ChangeUserRoleUseCase(
    IUserRepository userRepository,
    IAdminAuditLogRepository auditLogRepository)
{
    public async Task<UserAdminDto> ExecuteAsync(
        long targetUserId,
        long currentUserId,
        string role,
        CancellationToken ct = default)
    {
        if (targetUserId == currentUserId)
            throw new UnauthorizedRoleChangeException("Users cannot change their own role");

        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole))
            throw new InvalidUserRoleException();

        var user = await userRepository.FindByIdAsync(targetUserId, ct)
            ?? throw new UserNotFoundException();

        var previousRole = user.Role;
        user.Role = parsedRole;
        var updated = await userRepository.UpdateAsync(user, ct);

        await auditLogRepository.AddAsync(new AdminAuditLog
        {
            ActorUserId = currentUserId,
            TargetUserId = updated.Id,
            Action = "UserRoleChanged",
            Detail = $"Changed role for user '{updated.Email}' from '{previousRole}' to '{updated.Role}'.",
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new UserAdminDto(
            updated.Id,
            updated.Username,
            updated.Email,
            updated.Role.ToString(),
            updated.IsActive,
            updated.DateJoined);
    }
}
