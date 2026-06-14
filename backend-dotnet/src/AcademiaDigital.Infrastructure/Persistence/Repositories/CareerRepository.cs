using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CareerRepository(AppDbContext db) : ICareerRepository
{
    public async Task<IEnumerable<Career>> GetAllAsync(CancellationToken ct = default)
        => await db.Careers.AsNoTracking().ToListAsync(ct);

    public async Task<Career?> FindByIdAsync(int id, CancellationToken ct = default)
        => await db.Careers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Career?> FindByCodeAsync(string code, CancellationToken ct = default)
        => await db.Careers.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code, ct);

    public async Task<Career> CreateAsync(Career career, CancellationToken ct = default)
    {
        db.Careers.Add(career);
        await db.SaveChangesAsync(ct);
        return career;
    }

    public async Task<Career> UpdateAsync(Career career, CancellationToken ct = default)
    {
        db.Careers.Update(career);
        await db.SaveChangesAsync(ct);
        return career;
    }

    public async Task DeleteAsync(Career career, CancellationToken ct = default)
    {
        db.Careers.Remove(career);
        await db.SaveChangesAsync(ct);
    }
}
