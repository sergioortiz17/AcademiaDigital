using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class TeacherCareerRepository(AppDbContext db) : ITeacherCareerRepository
{
    public async Task EnsureAsync(long teacherId, int careerId, CancellationToken ct = default)
    {
        var exists = await db.TeacherCareers.AnyAsync(x => x.TeacherId == teacherId && x.CareerId == careerId, ct);
        if (exists) return;
        db.TeacherCareers.Add(new TeacherCareer { TeacherId = teacherId, CareerId = careerId, IsActive = true });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherCareer>> GetByTeacherAsync(long teacherId, CancellationToken ct = default)
        => await db.TeacherCareers.AsNoTracking().Include(x => x.Career)
            .Where(x => x.TeacherId == teacherId)
            .ToListAsync(ct);
}
