using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class DivisionRepository(AppDbContext db) : IDivisionRepository
{
    public Task<Division?> FindByIdAsync(int id, CancellationToken ct = default)
        => db.Divisions.AsNoTracking().FirstOrDefaultAsync(commission => commission.Id == id, ct);

    public async Task<IReadOnlyList<Division>> FindMatchingActiveAsync(
        int careerId, int academicYear, int yearNumber, string shift, CancellationToken ct = default)
        => await db.Divisions.AsNoTracking()
            .Where(c => c.IsActive
                && c.CareerId == careerId
                && c.AcademicYear == academicYear
                && c.YearNumber == yearNumber
                && c.Shift == shift)
            .ToListAsync(ct);
}
