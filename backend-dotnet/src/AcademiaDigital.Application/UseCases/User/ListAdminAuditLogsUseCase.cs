using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.User;

public class ListAdminAuditLogsUseCase(IAdminAuditLogRepository auditLogRepository)
{
    public async Task<IReadOnlyList<AdminAuditLogDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var logs = await auditLogRepository.ListAsync(ct);

        return logs
            .Select(a => new AdminAuditLogDto(
                a.Id,
                a.ActorUserId,
                a.ActorUser.Email,
                a.TargetUserId,
                a.TargetUser?.Email,
                a.Action,
                a.Detail,
                a.CreatedAt))
            .ToList();
    }
}

public record AdminAuditLogDto(
    long Id,
    long ActorUserId,
    string ActorEmail,
    long? TargetUserId,
    string? TargetEmail,
    string Action,
    string Detail,
    DateTime CreatedAt);
