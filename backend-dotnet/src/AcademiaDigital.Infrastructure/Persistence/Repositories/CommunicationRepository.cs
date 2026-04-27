using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CommunicationRepository(AppDbContext db) : ICommunicationRepository
{
    public async Task<IEnumerable<Communication>> GetAllAsync(CancellationToken ct = default)
        => await db.Communications.AsNoTracking()
            .Include(c => c.Author)
            .OrderByDescending(c => c.PublishedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Communication>> GetActiveByTypeAsync(CommunicationType type, CancellationToken ct = default)
        => await db.Communications.AsNoTracking()
            .Where(c => c.IsActive && c.Type == type && (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(c => c.PublishedAt)
            .ToListAsync(ct);

    public async Task<Communication?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.Communications.AsNoTracking()
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Communication> CreateAsync(Communication communication, CancellationToken ct = default)
    {
        db.Communications.Add(communication);
        await db.SaveChangesAsync(ct);
        return communication;
    }

    public async Task<Communication> UpdateAsync(Communication communication, CancellationToken ct = default)
    {
        db.Communications.Update(communication);
        await db.SaveChangesAsync(ct);
        return communication;
    }

    public async Task DeleteAsync(Communication communication, CancellationToken ct = default)
    {
        db.Communications.Remove(communication);
        await db.SaveChangesAsync(ct);
    }
}
