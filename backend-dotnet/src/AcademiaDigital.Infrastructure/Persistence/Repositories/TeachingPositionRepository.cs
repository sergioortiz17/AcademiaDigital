using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class TeachingPositionRepository(AppDbContext db) : ITeachingPositionRepository
{
    public async Task<IEnumerable<TeachingPosition>> GetByCourseAsync(int courseId, CancellationToken ct = default)
        => await db.TeachingPositions.AsNoTracking()
            .Include(tp => tp.Teacher).ThenInclude(t => t!.User)
            .Where(tp => tp.CourseId == courseId)
            .OrderBy(tp => tp.AcademicYear).ThenBy(tp => tp.Semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<TeachingPosition>> GetByTeacherAsync(long teacherId, CancellationToken ct = default)
        => await db.TeachingPositions.AsNoTracking()
            .Include(tp => tp.Course)
            .Where(tp => tp.TeacherId == teacherId)
            .OrderByDescending(tp => tp.AcademicYear).ThenByDescending(tp => tp.Semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<TeachingPosition>> GetByPeriodAsync(int year, int semester, CancellationToken ct = default)
        => await db.TeachingPositions.AsNoTracking()
            .Include(tp => tp.Course)
            .Include(tp => tp.Teacher).ThenInclude(t => t!.User)
            .Where(tp => tp.AcademicYear == year && tp.Semester == semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<TeachingPosition>> GetVacantAsync(CancellationToken ct = default)
        => await db.TeachingPositions.AsNoTracking()
            .Include(tp => tp.Course)
            .Where(tp => tp.IsVacant)
            .ToListAsync(ct);

    public async Task<TeachingPosition?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.TeachingPositions.AsNoTracking()
            .Include(tp => tp.Course)
            .Include(tp => tp.Teacher).ThenInclude(t => t!.User)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<TeachingPosition> CreateAsync(TeachingPosition position, CancellationToken ct = default)
    {
        db.TeachingPositions.Add(position);
        await db.SaveChangesAsync(ct);
        return position;
    }

    public async Task<TeachingPosition> UpdateAsync(TeachingPosition position, CancellationToken ct = default)
    {
        db.TeachingPositions.Update(position);
        await db.SaveChangesAsync(ct);
        return position;
    }

    public async Task DeleteAsync(TeachingPosition position, CancellationToken ct = default)
    {
        db.TeachingPositions.Remove(position);
        await db.SaveChangesAsync(ct);
    }
}
