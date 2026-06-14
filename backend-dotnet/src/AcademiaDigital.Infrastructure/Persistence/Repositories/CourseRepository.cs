using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CourseRepository(AppDbContext db) : ICourseRepository
{
    public async Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct = default)
        => await db.Courses.AsNoTracking().Include(c => c.Career).ToListAsync(ct);

    public async Task<IEnumerable<Course>> GetByCareerAsync(int careerId, CancellationToken ct = default)
        => await db.Courses.AsNoTracking()
            .Where(c => c.CareerId == careerId)
            .OrderBy(c => c.Code)
            .ToListAsync(ct);

    public async Task<Course?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.Courses.AsNoTracking()
            .Include(c => c.Career)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Course?> FindByCodeAsync(int careerId, string code, CancellationToken ct = default)
        => await db.Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CareerId == careerId && c.Code == code, ct);

    public async Task<bool> ExistsInCareerAsync(int courseId, int careerId, CancellationToken ct = default)
        => await db.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId && c.CareerId == careerId, ct);

    public async Task<Course> CreateAsync(Course course, CancellationToken ct = default)
    {
        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);
        return course;
    }

    public async Task<Course> UpdateAsync(Course course, CancellationToken ct = default)
    {
        db.Courses.Update(course);
        await db.SaveChangesAsync(ct);
        return course;
    }

    public async Task DeleteAsync(Course course, CancellationToken ct = default)
    {
        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);
    }
}
