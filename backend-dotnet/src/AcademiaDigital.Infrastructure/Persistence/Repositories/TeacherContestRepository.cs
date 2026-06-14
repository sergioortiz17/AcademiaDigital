using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class TeacherContestRepository(AppDbContext db) : ITeacherContestRepository
{
    public async Task<IEnumerable<TeacherContest>> GetAllAsync(CancellationToken ct = default)
        => await db.TeacherContests.AsNoTracking()
            .Include(tc => tc.Course)
            .Include(tc => tc.Career)
            .OrderByDescending(tc => tc.OpenDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<TeacherContest>> GetByStatusAsync(ContestStatus status, CancellationToken ct = default)
        => await db.TeacherContests.AsNoTracking()
            .Include(tc => tc.Course)
            .Include(tc => tc.Career)
            .Where(tc => tc.Status == status)
            .OrderByDescending(tc => tc.OpenDate)
            .ToListAsync(ct);

    public async Task<TeacherContest?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.TeacherContests.AsNoTracking()
            .Include(tc => tc.Course)
            .Include(tc => tc.Career)
            .Include(tc => tc.Applications).ThenInclude(a => a.Applicant)
            .FirstOrDefaultAsync(tc => tc.Id == id, ct);

    public async Task<TeacherContest> CreateAsync(TeacherContest contest, CancellationToken ct = default)
    {
        db.TeacherContests.Add(contest);
        await db.SaveChangesAsync(ct);
        return contest;
    }

    public async Task<TeacherContest> UpdateAsync(TeacherContest contest, CancellationToken ct = default)
    {
        db.TeacherContests.Update(contest);
        await db.SaveChangesAsync(ct);
        return contest;
    }

    public async Task DeleteAsync(TeacherContest contest, CancellationToken ct = default)
    {
        db.TeacherContests.Remove(contest);
        await db.SaveChangesAsync(ct);
    }
}
