using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class TeachingPositionRepository(AppDbContext db) : ITeachingPositionRepository
{
    public async Task<IReadOnlyList<TeachingPosition>> GetAllAsync(
        int? academicYear,
        int? semester,
        bool? isVacant,
        bool includeInactive,
        CancellationToken ct = default)
    {
        var query = Details().Where(position => includeInactive || position.IsActive);
        if (academicYear.HasValue) query = query.Where(position => position.AcademicYear == academicYear);
        if (semester.HasValue) query = query.Where(position => position.Semester == semester);
        if (isVacant.HasValue) query = query.Where(position => position.IsVacant == isVacant);
        return await query.OrderByDescending(position => position.AcademicYear)
            .ThenBy(position => position.Semester)
            .ThenBy(position => position.Course.Code)
            .ToArrayAsync(ct);
    }

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
        => await Details()
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<TeachingPosition> CreateAsync(TeachingPosition position, CancellationToken ct = default)
    {
        db.TeachingPositions.Add(position);
        await db.SaveChangesAsync(ct);
        return position;
    }

    public async Task<TeachingPosition> UpdateAsync(TeachingPosition position, CancellationToken ct = default)
    {
        db.Entry(position).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
        return position;
    }

    public async Task DeactivateAsync(TeachingPosition position, CancellationToken ct = default)
    {
        db.Entry(position).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<TeachingPosition> Details()
        => db.TeachingPositions.AsNoTracking()
            .Include(position => position.Course)
            .Include(position => position.Commission)
            .Include(position => position.Teacher).ThenInclude(teacher => teacher!.User);
}
