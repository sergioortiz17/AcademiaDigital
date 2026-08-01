using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class StudentRepository(AppDbContext db) : IStudentRepository
{
    public async Task<IEnumerable<Student>> GetAllAsync(CancellationToken ct = default)
        => await db.Students.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Career)
            .ToListAsync(ct);

    public async Task<IEnumerable<Student>> GetByCareerAsync(int careerId, CancellationToken ct = default)
        => await db.Students.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Career)
            .Where(s => s.Careers.Any(sc => sc.CareerId == careerId && sc.IsActive))
            .ToListAsync(ct);

    public async Task<Student?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.Students.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Career)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Student?> FindByUserIdAsync(long userId, CancellationToken ct = default)
        => await db.Students.AsNoTracking()
            .Include(s => s.Career)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<Student?> FindByLegajoAsync(string legajoNumber, CancellationToken ct = default)
        => await db.Students.AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.LegajoNumber == legajoNumber, ct);

    public async Task<Student> CreateAsync(Student student, CancellationToken ct = default)
    {
        db.Students.Add(student);
        await db.SaveChangesAsync(ct);
        return student;
    }

    public async Task<Student> UpdateAsync(Student student, CancellationToken ct = default)
    {
        db.Students.Update(student);
        await db.SaveChangesAsync(ct);
        return student;
    }

    public async Task DeleteAsync(Student student, CancellationToken ct = default)
    {
        db.Students.Remove(student);
        await db.SaveChangesAsync(ct);
    }
}
