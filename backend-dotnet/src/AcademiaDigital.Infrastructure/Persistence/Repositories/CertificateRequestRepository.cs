using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CertificateRequestRepository(AppDbContext db) : ICertificateRequestRepository
{
    public async Task<List<CertificateRequest>> GetByUserAsync(long userId, CancellationToken ct = default)
        => await db.CertificateRequests
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<CertificateRequest>> GetAllAsync(string? search, CertificateStatus? status, CancellationToken ct = default)
    {
        var query = db.CertificateRequests.AsNoTracking().Include(c => c.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.User.Username.ToLower().Contains(term) ||
                c.User.Email.ToLower().Contains(term));
        }

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
    }

    public async Task<CertificateRequest> CreateAsync(long userId, string certificateType, CancellationToken ct = default)
    {
        var request = new CertificateRequest
        {
            UserId = userId,
            CertificateType = certificateType,
            Status = CertificateStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        db.CertificateRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request;
    }

    public async Task<CertificateRequest?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.CertificateRequests.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<CertificateRequest> UpdateStatusAsync(long id, CertificateStatus status, CancellationToken ct = default)
    {
        var request = await db.CertificateRequests.FindAsync([id], ct)
            ?? throw new UserNotFoundException(id);
        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return request;
    }
}
