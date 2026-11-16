using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence.Control;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Tenancy;

public sealed class PlatformRequestStore(ControlDbContext control) : IPlatformRequestStore
{
    public async Task AddAsync(PlatformRequest request, CancellationToken ct = default)
    {
        control.PlatformRequests.Add(request);
        await control.SaveChangesAsync(ct);
    }

    public Task<PlatformRequest?> FindAsync(Guid id, CancellationToken ct = default)
        => control.PlatformRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<PlatformRequest>> ListAsync(PlatformRequestStatus? status, CancellationToken ct = default)
    {
        var query = control.PlatformRequests.AsNoTracking();
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return await query.OrderByDescending(r => r.CreatedAt).Take(500).ToListAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => control.SaveChangesAsync(ct);
}
