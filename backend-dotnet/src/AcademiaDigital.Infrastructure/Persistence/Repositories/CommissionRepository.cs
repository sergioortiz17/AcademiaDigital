using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class CommissionRepository(AppDbContext db) : ICommissionRepository
{
    public Task<Commission?> FindByIdAsync(int id, CancellationToken ct = default)
        => db.Commissions.AsNoTracking().FirstOrDefaultAsync(commission => commission.Id == id, ct);
}
