using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class StudyPlanRepository(AppDbContext db) : IStudyPlanRepository
{
    public async Task<IEnumerable<StudyPlan>> GetByCareerIdAsync(int careerId, CancellationToken ct = default)
        => await db.StudyPlans.AsNoTracking()
            .Where(sp => sp.CareerId == careerId)
            .OrderByDescending(sp => sp.VersionNumber)
            .ToListAsync(ct);

    public async Task<StudyPlan?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.StudyPlans.AsNoTracking()
            .Include(sp => sp.Career)
            .FirstOrDefaultAsync(sp => sp.Id == id, ct);

    public async Task<StudyPlan?> GetActiveByCareerIdAsync(int careerId, CancellationToken ct = default)
        => await db.StudyPlans.AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.CareerId == careerId && sp.Status == StudyPlanStatus.Active && sp.IsActive, ct);

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => await db.StudyPlans.AsNoTracking().AnyAsync(sp => sp.Id == id, ct);

    public async Task<StudyPlan> CreateAsync(StudyPlan studyPlan, CancellationToken ct = default)
    {
        db.StudyPlans.Add(studyPlan);
        await db.SaveChangesAsync(ct);
        return studyPlan;
    }

    public async Task<StudyPlan> UpdateAsync(StudyPlan studyPlan, CancellationToken ct = default)
    {
        db.StudyPlans.Update(studyPlan);
        await db.SaveChangesAsync(ct);
        return studyPlan;
    }
}
