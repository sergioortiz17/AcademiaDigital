using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class EnrollmentPeriodRepository(AppDbContext db) : IEnrollmentPeriodRepository
{
    public async Task<IEnumerable<EnrollmentPeriod>> GetAllAsync(CancellationToken ct = default)
        => await db.EnrollmentPeriods.AsNoTracking()
            .Include(ep => ep.Career)
            .Include(ep => ep.StudyPlan)
            .OrderByDescending(ep => ep.StartDate)
            .ToListAsync(ct);

    public async Task<EnrollmentPeriod?> GetActiveByCareerAsync(int careerId, CancellationToken ct = default)
        => await db.EnrollmentPeriods.AsNoTracking()
            .Include(ep => ep.Career)
            .Include(ep => ep.StudyPlan)
            .FirstOrDefaultAsync(ep => ep.CareerId == careerId && ep.IsActive, ct);

    public async Task<EnrollmentPeriod?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.EnrollmentPeriods.AsNoTracking()
            .Include(ep => ep.Career)
            .Include(ep => ep.StudyPlan)
            .FirstOrDefaultAsync(ep => ep.Id == id, ct);

    public async Task<(int Morning, int Afternoon, int Evening)> GetEnrolledShiftCountsAsync(int periodId, CancellationToken ct = default)
    {
        var counts = await db.Enrollments
            .Where(e => e.EnrollmentPeriodId == periodId)
            .GroupBy(e => e.Shift)
            .Select(g => new { Shift = g.Key, Count = g.Select(e => e.StudentId).Distinct().Count() })
            .ToListAsync(ct);

        int Get(string shift) => counts.FirstOrDefault(c => c.Shift == shift)?.Count ?? 0;
        return (Get("Mañana"), Get("Tarde"), Get("Noche"));
    }

    public async Task<IReadOnlyDictionary<int, (int Morning, int Afternoon, int Evening)>> GetAllEnrolledShiftCountsAsync(
        IEnumerable<int> periodIds, CancellationToken ct = default)
    {
        var ids = periodIds.ToList();
        var rows = await db.Enrollments
            .Where(e => e.EnrollmentPeriodId != null && ids.Contains(e.EnrollmentPeriodId.Value))
            .GroupBy(e => new { e.EnrollmentPeriodId, e.Shift })
            .Select(g => new { g.Key.EnrollmentPeriodId, g.Key.Shift, Count = g.Select(e => e.StudentId).Distinct().Count() })
            .ToListAsync(ct);

        return ids.ToDictionary(
            id => id,
            id => (
                Morning: rows.FirstOrDefault(r => r.EnrollmentPeriodId == id && r.Shift == "Mañana")?.Count ?? 0,
                Afternoon: rows.FirstOrDefault(r => r.EnrollmentPeriodId == id && r.Shift == "Tarde")?.Count ?? 0,
                Evening: rows.FirstOrDefault(r => r.EnrollmentPeriodId == id && r.Shift == "Noche")?.Count ?? 0
            ));
    }

    public async Task<EnrollmentPeriod> CreateAsync(EnrollmentPeriod period, CancellationToken ct = default)
    {
        db.EnrollmentPeriods.Add(period);
        await db.SaveChangesAsync(ct);
        // Reload with navigation properties so Mapper.Map doesn't throw
        return await db.EnrollmentPeriods
            .AsNoTracking()
            .Include(ep => ep.Career)
            .Include(ep => ep.StudyPlan)
            .FirstAsync(ep => ep.Id == period.Id, ct);
    }

    public async Task<EnrollmentPeriod> UpdateAsync(EnrollmentPeriod period, CancellationToken ct = default)
    {
        db.EnrollmentPeriods.Update(period);
        await db.SaveChangesAsync(ct);
        return period;
    }

    public async Task DeleteAsync(int periodId, CancellationToken ct = default)
    {
        var period = await db.EnrollmentPeriods.FindAsync([periodId], ct)
            ?? throw new KeyNotFoundException("Enrollment period not found.");
        db.EnrollmentPeriods.Remove(period);
        await db.SaveChangesAsync(ct);
    }
}
