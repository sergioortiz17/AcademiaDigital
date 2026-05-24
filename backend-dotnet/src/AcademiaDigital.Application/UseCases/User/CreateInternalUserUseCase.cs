using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

public class CreateInternalUserUseCase(
    IUserRepository userRepository,
    IAdminAuditLogRepository auditLogRepository)
{
    public async Task<UserAdminDto> ExecuteAsync(
        long actorUserId,
        string email,
        string username,
        string password,
        string role,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole))
            throw new InvalidUserRoleException();

        var existing = await userRepository.FindByEmailAsync(email, ct);
        if (existing is not null)
            throw new EmailAlreadyExistsException();

        var user = await userRepository.CreateAsync(email, username, password, parsedRole, ct);

        await auditLogRepository.AddAsync(new AdminAuditLog
        {
            ActorUserId = actorUserId,
            TargetUserId = user.Id,
            Action = "InternalUserCreated",
            Detail = $"Created internal user '{user.Email}' with role '{user.Role}'.",
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new UserAdminDto(
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString(),
            user.IsActive,
            user.DateJoined);
    }
}
