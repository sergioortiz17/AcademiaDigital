using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IAdminAuditLogRepository
{
    Task<IReadOnlyList<AdminAuditLog>> ListAsync(CancellationToken ct = default);
    Task AddAsync(AdminAuditLog auditLog, CancellationToken ct = default);
}
