using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class SessionRepository(AppDbContext db) : ISessionRepository
{
    public async Task<ActiveSession?> FindByTokenAsync(string token, CancellationToken ct = default)
        => await db.ActiveSessions
            .Include(s => s.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == token, ct);

    public async Task<ActiveSession?> FindByUserAsync(long userId, CancellationToken ct = default)
        => await db.ActiveSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<ActiveSession> CreateAsync(long userId, string token, CancellationToken ct = default)
    {
        var session = new ActiveSession
        {
            UserId = userId,
            Token = token,
            CreatedAt = DateTime.UtcNow
        };

        db.ActiveSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task DeleteAsync(ActiveSession session, CancellationToken ct = default)
    {
        db.ActiveSessions.Remove(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteByUserAsync(long userId, CancellationToken ct = default)
    {
        await db.ActiveSessions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }
}
