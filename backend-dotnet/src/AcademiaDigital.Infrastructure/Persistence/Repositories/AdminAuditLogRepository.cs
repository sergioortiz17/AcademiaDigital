using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class AdminAuditLogRepository(AppDbContext db) : IAdminAuditLogRepository
{
    public async Task<IReadOnlyList<AdminAuditLog>> ListAsync(CancellationToken ct = default)
        => await db.AdminAuditLogs
            .AsNoTracking()
            .Include(a => a.ActorUser)
            .Include(a => a.TargetUser)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync(ct);

    public async Task AddAsync(AdminAuditLog auditLog, CancellationToken ct = default)
    {
        db.AdminAuditLogs.Add(auditLog);
        await db.SaveChangesAsync(ct);
    }
}
