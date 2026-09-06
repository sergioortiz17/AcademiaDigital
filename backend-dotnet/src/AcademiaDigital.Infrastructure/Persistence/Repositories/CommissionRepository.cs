using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class CommissionRepository(AppDbContext db) : ICommissionRepository
{
    public Task<Commission?> FindByIdAsync(int id, CancellationToken ct = default)
        => db.Commissions.AsNoTracking().FirstOrDefaultAsync(commission => commission.Id == id, ct);

    public async Task<IReadOnlyList<Commission>> FindMatchingActiveAsync(
        int careerId, int academicYear, int yearNumber, string shift, CancellationToken ct = default)
        => await db.Commissions.AsNoTracking()
            .Where(c => c.IsActive
                && c.CareerId == careerId
                && c.AcademicYear == academicYear
                && c.YearNumber == yearNumber
                && c.Shift == shift)
            .ToListAsync(ct);
}
