using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class StudentCareerRepository(AppDbContext db) : IStudentCareerRepository
{
    public async Task<IReadOnlyList<StudentCareer>> GetByStudentAsync(long studentId, CancellationToken ct = default)
        => await db.StudentCareers.AsNoTracking().Include(x => x.Career)
            .Where(x => x.StudentId == studentId).OrderByDescending(x => x.IsActive).ThenBy(x => x.EnrollmentDate).ToListAsync(ct);

    public Task<StudentCareer?> FindAsync(long studentId, int careerId, bool activeOnly = true, CancellationToken ct = default)
        => db.StudentCareers.Include(x => x.Career)
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.CareerId == careerId && (!activeOnly || x.IsActive), ct);

    public async Task<StudentCareer> CreateAsync(StudentCareer studentCareer, CancellationToken ct = default)
    {
        db.StudentCareers.Add(studentCareer);
        await db.SaveChangesAsync(ct);
        return studentCareer;
    }
}
